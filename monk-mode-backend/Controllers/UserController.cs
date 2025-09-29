using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using monk_mode_backend.Domain;
using monk_mode_backend.Models;

namespace monk_mode_backend.Controllers
{
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

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var userProfile = new UserProfileDTO
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                XP = user.Xp,
                Level = user.Level
            };

            return Ok(userProfile);
        }

        [HttpPost("updatexp")]
        public async Task<IActionResult> UpdateXP([FromBody] UpdateXpRequestDTO request) {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            user.Xp += request.XpToAdd;

            // Level up logic using the dynamic XP requirement
            while (user.Xp >= GetRequiredXpForNextLevel(user.Level)) {
                user.Xp -= GetRequiredXpForNextLevel(user.Level);
                user.Level++;
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded) {
                return Ok(new { Message = "XP updated successfully", user.Xp, user.Level });
            } else {
                return BadRequest(new { result.Errors });
            }
        }

        private int GetRequiredXpForNextLevel(int currentLevel) {
            return 3000 + (currentLevel * 100);
        }
    }
}