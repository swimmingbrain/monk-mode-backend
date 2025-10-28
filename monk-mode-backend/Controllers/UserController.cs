using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using monk_mode_backend.Domain;
using monk_mode_backend.Models;   // ResponseDTO, UpdateXpRequestDTO
using monk_mode_backend.DTOs;     // UserProfileDTO

namespace monk_mode_backend.Controllers
{
    /// <summary>
    /// UserController – secure, naming-consistent ("Xp"), null-safe.
    /// - GET /api/user/profile   -> returns UserProfileDTO
    /// - POST /api/user/updatexp -> adds Xp and handles level-up
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET /api/user/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            // Manual map to ensure naming consistency (Xp, not XP)
            var dto = new UserProfileDTO
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Xp = user.Xp,
                Level = user.Level
            };

            return Ok(dto);
        }

        // POST /api/user/updatexp
        // Adds Xp to the current user and applies level-up rules.
        [HttpPost("updatexp")]
        public async Task<IActionResult> UpdateXp([FromBody] UpdateXpRequestDTO request)
        {
            if (request == null)
                return BadRequest(new ResponseDTO { Status = "error", Message = "Invalid payload." });

            // Optional: Basic validation
            if (request.XpToAdd == 0)
                return BadRequest(new ResponseDTO { Status = "error", Message = "XpToAdd must be non-zero." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            // Update Xp and apply level-up thresholds
            user.Xp += request.XpToAdd;

            // Same rule as your previous controller, only naming fixed to Xp:
            while (user.Xp >= GetRequiredXpForNextLevel(user.Level))
            {
                user.Xp -= GetRequiredXpForNextLevel(user.Level);
                user.Level++;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                return BadRequest(new ResponseDTO { Status = "error", Message = message });
            }

            // Lightweight success payload
            return Ok(new
            {
                Status = "success",
                Message = "Xp updated successfully.",
                Xp = user.Xp,
                Level = user.Level
            });
        }

        // Keep your XP requirement rule centralized here
        private static int GetRequiredXpForNextLevel(int currentLevel)
        {
            // Same formula as in your existing code: 3000 + (currentLevel * 100)
            return 3000 + (currentLevel * 100);
        }
    }
}
