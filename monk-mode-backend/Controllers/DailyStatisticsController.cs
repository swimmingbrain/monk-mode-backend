using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

using monk_mode_backend.Domain;           // ApplicationUser
using monk_mode_backend.Infrastructure;   // MonkModeDbContext
using monk_mode_backend.DTOs;             // DailyStatisticsDTO
using monk_mode_backend.Models;           // ResponseDTO

namespace monk_mode_backend.Controllers
{
    /// <summary>
    /// Changes (security & robustness):
    /// - [ApiController] + [Authorize]: all endpoints require a logged-in user.
    /// - Removed dangerous "log every header" behavior to avoid leaking Authorization tokens.
    /// - Read-only: returns user's own daily stats; write/compute happens server-side elsewhere.
    /// - Date handling: normalizes to UTC date-only semantics (server-side consistency).
    /// - Uses ResponseDTO { Status, Message } for error responses (404/400).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DailyStatisticsController : ControllerBase
    {
        private readonly MonkModeDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DailyStatisticsController> _logger;

        public DailyStatisticsController(
            MonkModeDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<DailyStatisticsController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Returns the current user's daily statistics for "today" (UTC-normalized).
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<DailyStatisticsDTO>> GetToday()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });
            }

            var todayUtc = DateTime.UtcNow.Date;

            var stat = await _db.DailyStatistics
                .AsNoTracking()
                .Where(s => s.UserId == user.Id && s.Date == todayUtc)
                .SingleOrDefaultAsync();

            if (stat is null)
            {
                return NotFound(new ResponseDTO
                {
                    Status = "error",
                    Message = "No daily statistics found for today."
                });
            }

            return Ok(ToDto(stat));
        }

        /// <summary>
        /// Returns the current user's daily statistics within a date range (inclusive).
        /// Dates must be ISO-8601 yyyy-MM-dd; they are treated as UTC dates.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyStatisticsDTO>>> GetRange(
            [FromQuery] string from,
            [FromQuery] string to)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });
            }

            if (!TryParseDate(from, out var fromDateUtc) || !TryParseDate(to, out var toDateUtc))
            {
                return BadRequest(new ResponseDTO
                {
                    Status = "error",
                    Message = "Parameters 'from' and 'to' must be valid dates in format yyyy-MM-dd."
                });
            }

            if (toDateUtc < fromDateUtc)
            {
                return BadRequest(new ResponseDTO
                {
                    Status = "error",
                    Message = "'to' must be greater than or equal to 'from'."
                });
            }

            var items = await _db.DailyStatistics
                .AsNoTracking()
                .Where(s => s.UserId == user.Id
                            && s.Date >= fromDateUtc
                            && s.Date <= toDateUtc)
                .OrderBy(s => s.Date)
                .ToListAsync();

            return Ok(items.Select(ToDto));
        }

        // -----------------------
        // Helpers
        // -----------------------

        /// <summary>
        /// Parses yyyy-MM-dd as a UTC date (DateTime with time 00:00 UTC).
        /// </summary>
        private static bool TryParseDate(string input, out DateTime utcDate)
        {
            utcDate = default;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Accept only yyyy-MM-dd to keep it unambiguous and DB-index friendly.
            if (DateTime.TryParseExact(input, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal |
                    System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out var dt))
            {
                utcDate = dt.Date; // normalize to date-only UTC
                return true;
            }
            return false;
        }

        private static DailyStatisticsDTO ToDto(DailyStatistics s) => new DailyStatisticsDTO
        {
            UserId = s.UserId,
            Date = DateOnly.FromDateTime(s.Date),
            TotalFocusTime = s.TotalFocusTime,
            Xp = s.Xp,
            Level = s.Level
        };
    }
}