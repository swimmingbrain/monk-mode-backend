using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;
using monk_mode_backend.Infrastructure;
using monk_mode_backend.Models;

namespace monk_mode_backend.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TemplateBlockController : ControllerBase {
        private readonly MonkModeDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public TemplateBlockController(MonkModeDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper) {
            _dbContext = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        // POST /template-blocks
        [HttpPost]
        public async Task<IActionResult> CreateTemplateBlock([FromBody] TemplateBlockDTO templateBlockData) {
            if (templateBlockData == null)
                return BadRequest("Invalid template block data.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Verify that the template belongs to the user
            var template = await _dbContext.Templates
                .FirstOrDefaultAsync(t => t.Id == templateBlockData.TemplateId && t.UserId == user.Id);

            if (template == null)
                return NotFound("Template not found or you don't have access to it.");

            var templateBlock = _mapper.Map<TemplateBlock>(templateBlockData);
            _dbContext.TemplateBlocks.Add(templateBlock);
            await _dbContext.SaveChangesAsync();

            var templateBlockDTO = _mapper.Map<TemplateBlockDTO>(templateBlock);
            return CreatedAtAction(nameof(GetTemplateBlockById), new { id = templateBlock.Id }, templateBlockDTO);
        }

        // GET /template-blocks/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTemplateBlockById(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // <- nach dem Guard: non-null, also in lokale Variable ziehen
            var userId = user.Id;

            var templateBlock = await _dbContext.TemplateBlocks
                .Include(tb => tb.Template)
                .Where(tb => tb.Id == id && tb.Template != null && tb.Template.UserId == userId)
                .FirstOrDefaultAsync();

            if (templateBlock == null)
                return NotFound();

            var templateBlockDTO = _mapper.Map<TemplateBlockDTO>(templateBlock);
            return Ok(templateBlockDTO);
        }


        // DELETE /template-blocks/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTemplateBlock(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var userId = user.Id; // nach Null-Guard

            var templateBlock = await _dbContext.TemplateBlocks
                .Include(tb => tb.Template)
                .Where(tb => tb.Id == id && tb.Template != null && tb.Template.UserId == userId)
                .FirstOrDefaultAsync();

            if (templateBlock == null)
                return NotFound();

            _dbContext.TemplateBlocks.Remove(templateBlock);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

    }
} 