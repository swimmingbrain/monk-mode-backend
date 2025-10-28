using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;
using monk_mode_backend.Infrastructure;
using monk_mode_backend.Models;

namespace monk_mode_backend.Controllers
{
    /// <summary>
    /// TimeBlockController – aligned to your DTO and domain.
    /// - Uses ResponseDTO for error cases.
    /// - Only fields present in TimeBlockDTO are used (Title, Date, StartTime, EndTime, IsFocus).
    /// - Server-enforced ownership: UserId is set from the authenticated user.
    /// - Ignores client-provided Tasks and UserId on write (overposting guard).
    /// - Null-safety: userId captured after null-guard to avoid warnings.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TimeBlockController : ControllerBase
    {
        private readonly MonkModeDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public TimeBlockController(
            MonkModeDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _mapper = mapper;
        }

        // GET /timeblocks
        [HttpGet]
        public async Task<IActionResult> GetAllTimeBlocks()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var blocks = await _dbContext.TimeBlocks
                .Where(tb => tb.UserId == userId)
                .OrderByDescending(tb => tb.Date)
                .ThenByDescending(tb => tb.StartTime)
                .ToListAsync();

            var dto = _mapper.Map<List<TimeBlockDTO>>(blocks);
            return Ok(dto);
        }

        // GET /timeblocks/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTimeBlockById(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var block = await _dbContext.TimeBlocks
                .FirstOrDefaultAsync(tb => tb.Id == id && tb.UserId == userId);

            if (block == null)
                return NotFound(new ResponseDTO { Status = "error", Message = "Time block not found." });

            var dto = _mapper.Map<TimeBlockDTO>(block);
            return Ok(dto);
        }

        // POST /timeblocks
        [HttpPost]
        public async Task<IActionResult> CreateTimeBlock([FromBody] TimeBlockDTO dto)
        {
            if (dto == null)
                return BadRequest(new ResponseDTO { Status = "error", Message = "Invalid request body." });

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new ResponseDTO { Status = "error", Message = "Title is required." });

            if (dto.Date == default)
                return BadRequest(new ResponseDTO { Status = "error", Message = "Date is required." });

            if (dto.StartTime == default || dto.EndTime == default)
                return BadRequest(new ResponseDTO { Status = "error", Message = "StartTime and EndTime are required." });

            if (dto.EndTime <= dto.StartTime)
                return BadRequest(new ResponseDTO { Status = "error", Message = "EndTime must be after StartTime." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            // Manual mapping to avoid overposting (ignore dto.Tasks, dto.UserId)
            var entity = new TimeBlock
            {
                // Id = 0 by default
                UserId = userId,
                Title = dto.Title,
                Date = dto.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsFocus = dto.IsFocus
            };

            _dbContext.TimeBlocks.Add(entity);
            await _dbContext.SaveChangesAsync();

            var result = _mapper.Map<TimeBlockDTO>(entity);
            return CreatedAtAction(nameof(GetTimeBlockById), new { id = entity.Id }, result);
        }

        // PUT /timeblocks/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTimeBlock(int id, [FromBody] TimeBlockDTO dto)
        {
            if (dto == null)
                return BadRequest(new ResponseDTO { Status = "error", Message = "Invalid request body." });

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new ResponseDTO { Status = "error", Message = "Title is required." });

            if (dto.Date == default)
                return BadRequest(new ResponseDTO { Status = "error", Message = "Date is required." });

            if (dto.StartTime == default || dto.EndTime == default)
                return BadRequest(new ResponseDTO { Status = "error", Message = "StartTime and EndTime are required." });

            if (dto.EndTime <= dto.StartTime)
                return BadRequest(new ResponseDTO { Status = "error", Message = "EndTime must be after StartTime." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var block = await _dbContext.TimeBlocks
                .FirstOrDefaultAsync(tb => tb.Id == id && tb.UserId == userId);

            if (block == null)
                return NotFound(new ResponseDTO { Status = "error", Message = "Time block not found." });

            // Update only allowed fields; ignore client-provided UserId/Tasks
            block.Title = dto.Title;
            block.Date = dto.Date;
            block.StartTime = dto.StartTime;
            block.EndTime = dto.EndTime;
            block.IsFocus = dto.IsFocus;

            _dbContext.TimeBlocks.Update(block);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /timeblocks/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTimeBlock(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            var userId = user.Id;

            var block = await _dbContext.TimeBlocks
                .FirstOrDefaultAsync(tb => tb.Id == id && tb.UserId == userId);

            if (block == null)
                return NotFound(new ResponseDTO { Status = "error", Message = "Time block not found." });

            _dbContext.TimeBlocks.Remove(block);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
