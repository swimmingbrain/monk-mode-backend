using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;
using monk_mode_backend.Infrastructure;
using monk_mode_backend.Models;
using Microsoft.Extensions.Logging;

namespace monk_mode_backend.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DailyStatisticsController : ControllerBase {
        private readonly MonkModeDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ILogger<DailyStatisticsController> _logger;

        public DailyStatisticsController(
            MonkModeDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IMapper mapper,
            ILogger<DailyStatisticsController> logger) {
            _dbContext = context;
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        // GET /daily-statistics
        [HttpGet]
        public async Task<IActionResult> GetDailyStatistics([FromQuery] DateTime? date, [FromQuery] string? friendId) {
            try {
                _logger.LogInformation($"Received request - Date: {date}, FriendId: {friendId}");
                _logger.LogInformation($"Request headers: {string.Join(", ", Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");

                var user = await _userManager.GetUserAsync(User);
                if (user == null) {
                    _logger.LogWarning("Unauthorized access attempt - no user found");
                    return Unauthorized();
                }

                _logger.LogInformation($"Authenticated user: {user.Id}");

                string targetUserId = user.Id;
                ApplicationUser targetUser = user;

                // If friendId is provided, verify friendship and use friend's ID
                if (!string.IsNullOrEmpty(friendId)) {
                    _logger.LogInformation($"Checking friendship between user {user.Id} and friend {friendId}");

                    // Verify friend exists
                    var friend = await _userManager.FindByIdAsync(friendId);
                    if (friend == null) {
                        _logger.LogWarning($"Friend with ID {friendId} not found");
                        return NotFound($"Friend with ID {friendId} not found");
                    }

                    _logger.LogInformation($"Found friend: {friend.UserName}");

                    // Check friendship in both directions
                    var friendship = await _dbContext.Friendships
                        .FirstOrDefaultAsync(f => 
                            f.Status == "Accepted" && 
                            ((f.UserId == user.Id && f.FriendId == friendId) || 
                             (f.UserId == friendId && f.FriendId == user.Id)));

                    if (friendship == null) {
                        _logger.LogWarning($"No friendship found between user {user.Id} and friend {friendId}");
                        return Forbid("You are not friends with this user.");
                    }

                    _logger.LogInformation($"Friendship verified between user {user.Id} and friend {friendId}");
                    targetUserId = friendId;
                    targetUser = friend;
                }

                var query = _dbContext.DailyStatistics
                    .Where(ds => ds.UserId == targetUserId);

                if (date.HasValue) {
                    var startOfDay = date.Value.Date;
                    var endOfDay = startOfDay.AddDays(1);
                    query = query.Where(ds => ds.Date >= startOfDay && ds.Date < endOfDay);
                    _logger.LogInformation($"Filtering statistics for date range: {startOfDay} to {endOfDay}");
                }

                var statistics = await query.ToListAsync();
                var statisticsDTOs = _mapper.Map<List<DailyStatisticsDTO>>(statistics);

                // Add user information to each DTO
                foreach (var dto in statisticsDTOs) {
                    dto.Username = targetUser.UserName;
                    dto.Xp = targetUser.Xp;
                    dto.Level = targetUser.Level;
                }

                _logger.LogInformation($"Retrieved {statisticsDTOs.Count} statistics records for user {targetUserId}");
                return Ok(statisticsDTOs);
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Error fetching daily statistics. Date: {date}, FriendId: {friendId}");
                return StatusCode(500, "An error occurred while fetching statistics");
            }
        }

        // POST /daily-statistics/update
        [HttpPost("update")]
        public async Task<IActionResult> UpdateDailyStatistics([FromBody] DailyStatisticsDTO statisticsData) {
            try {
                if (statisticsData == null) {
                    _logger.LogWarning("Invalid statistics data received");
                    return BadRequest("Invalid statistics data.");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null) {
                    _logger.LogWarning("Unauthorized access attempt - no user found");
                    return Unauthorized();
                }

                var startOfDay = statisticsData.Date.Date;
                var endOfDay = startOfDay.AddDays(1);

                var existingStats = await _dbContext.DailyStatistics
                    .FirstOrDefaultAsync(ds => ds.UserId == user.Id && ds.Date >= startOfDay && ds.Date < endOfDay);

                if (existingStats == null) {
                    // Create new statistics for the day
                    var newStats = _mapper.Map<DailyStatistics>(statisticsData);
                    newStats.UserId = user.Id;
                    _dbContext.DailyStatistics.Add(newStats);
                    _logger.LogInformation($"Created new statistics for user {user.Id} on {startOfDay}");
                } else {
                    // Add to existing statistics instead of overwriting
                    existingStats.TotalFocusTime += statisticsData.TotalFocusTime;
                    _logger.LogInformation($"Updated existing statistics for user {user.Id} on {startOfDay}. New total: {existingStats.TotalFocusTime}");
                }

                await _dbContext.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error updating daily statistics");
                return StatusCode(500, "An error occurred while updating statistics");
            }
        }
    }
} 