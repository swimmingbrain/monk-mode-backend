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
    public class TemplateController : ControllerBase {
        private readonly MonkModeDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public TemplateController(MonkModeDbContext context, UserManager<ApplicationUser> userManager, IMapper mapper) {
            _dbContext = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        // GET /templates
        [HttpGet]
        public async Task<IActionResult> GetAllTemplates() {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var templates = await _dbContext.Templates
                .Where(t => t.UserId == user.Id)
                .Include(t => t.TemplateBlocks)
                .ToListAsync();

            var templateDTOs = _mapper.Map<List<TemplateDTO>>(templates);
            return Ok(templateDTOs);
        }

        // POST /templates
        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] TemplateDTO templateData) {
            if (templateData == null)
                return BadRequest("Invalid template data.");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var template = _mapper.Map<Template>(templateData);
            template.UserId = user.Id;

            _dbContext.Templates.Add(template);
            await _dbContext.SaveChangesAsync();

            var templateDTO = _mapper.Map<TemplateDTO>(template);
            return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, templateDTO);
        }

        // GET /templates/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplateById(int id) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var template = await _dbContext.Templates
                .Include(t => t.TemplateBlocks)
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Id == id);

            if (template == null)
                return NotFound();

            var templateDTO = _mapper.Map<TemplateDTO>(template);
            return Ok(templateDTO);
        }

        // DELETE /templates/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var template = await _dbContext.Templates
                .Include(t => t.TemplateBlocks)
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Id == id);

            if (template == null)
                return NotFound();

            _dbContext.Templates.Remove(template);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
} 