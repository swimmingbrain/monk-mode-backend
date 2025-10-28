using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;
using monk_mode_backend.Infrastructure;
using monk_mode_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace monk_mode_backend.Controllers
{
    /// <summary>
    /// TemplateController – aligned to current DTOs and hardened:
    /// - Consistent ResponseDTO for error cases.
    /// - Ownership enforced: templates are always scoped to the authenticated user.
    /// - Overposting guard: ignore client-provided TemplateBlocks on create/update.
    /// - Null-safety: capture userId after null-guard to avoid nullable warnings.
    /// - CreatedAt set server-side on create.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TemplateController : ControllerBase
    {
        private readonly MonkModeDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public TemplateController(
            MonkModeDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _mapper = mapper;
        }

        // GET /templates
        [HttpGet]
        public async Task<IActionResult> GetAllTemplates()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var templates = await _dbContext.Templates
                .Where(t => t.UserId == userId)
                .Include(t => t.TemplateBlocks)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Map entity -> DTO (DTO contains CreatedAt and TemplateBlocks)
            var dto = _mapper.Map<List<TemplateDTO>>(templates);
            return Ok(dto);
        }

        // GET /templates/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTemplateById(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var template = await _dbContext.Templates
                .Include(t => t.TemplateBlocks)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (template == null)
                return NotFound(new ResponseDTO { Status = "error", Message = "Template not found." });

            var result = _mapper.Map<TemplateDTO>(template);
            return Ok(result);
        }

        // POST /templates
        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] TemplateDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new ResponseDTO { Status = "error", Message = "Title is required." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            // Overposting guard: ignore incoming TemplateBlocks on create
            var entity = new Template
            {
                // Id is DB-generated
                UserId = userId,
                Title = dto.Title,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Templates.Add(entity);
            await _dbContext.SaveChangesAsync();

            var result = _mapper.Map<TemplateDTO>(entity);
            // Note: result.TemplateBlocks will be null/empty unless you load them later (by design)
            return CreatedAtAction(nameof(GetTemplateById), new { id = entity.Id }, result);
        }

        // PUT /templates/{id}
        // Update title only (blocks managed via TemplateBlockController)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] TemplateDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new ResponseDTO { Status = "error", Message = "Title is required." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var template = await _dbContext.Templates
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (template == null)
                return NotFound(new ResponseDTO { Status = "error", Message = "Template not found." });

            // Overposting guard: do not accept client-provided TemplateBlocks here
            template.Title = dto.Title;

            _dbContext.Templates.Update(template);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /templates/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var template = await _dbContext.Templates
                .Include(t => t.TemplateBlocks)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (template == null)
                return NotFound(new ResponseDTO { Status = "error", Message = "Template not found." });

            _dbContext.Templates.Remove(template);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}