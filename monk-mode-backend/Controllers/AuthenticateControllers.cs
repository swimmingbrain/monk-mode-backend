using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using monk_mode_backend.Application;      // ITokenService
using monk_mode_backend.Domain;          // ApplicationUser
using monk_mode_backend.DTOs;            // LoginDTO, RegisterDTO, TokenDTO
using monk_mode_backend.Models;          // ResponseDTO

namespace monk_mode_backend.Controllers
{
    /// <summary>
    /// Changes (security & robustness):
    /// - Uses ResponseDTO { Status, Message } for non-OK flows (conflict, bad request, unauthorized, logout).
    /// - Keeps TokenDTO on successful login (client expects token shape).
    /// - [ApiController] enables automatic 400 on invalid DTOs; we also provide explicit 409/401/201 where relevant.
    /// - Generic error messages to avoid user enumeration.
    /// - Pure Bearer (no cookies), ready for future refresh-token extension.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthenticateController> _logger;

        public AuthenticateController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            ILogger<AuthenticateController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user.
        /// Returns 201 on success with ResponseDTO, or 409/400 with ResponseDTO on failure.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            // [ApiController] will auto-return 400 for invalid ModelState (DataAnnotations).
            // Additional semantic checks below use ResponseDTO.

            var existingByName = await _userManager.FindByNameAsync(dto.Username);
            if (existingByName != null)
            {
                return Conflict(new ResponseDTO
                {
                    Status = "error",
                    Message = "Username is already taken."
                });
            }

            var existingByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingByEmail != null)
            {
                return Conflict(new ResponseDTO
                {
                    Status = "error",
                    Message = "Email is already registered."
                });
            }

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email
                // Level/Xp/CreatedAt defaults are set in the entity
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                _logger.LogWarning("Registration failed for {Email}: {Errors}", dto.Email, string.Join("; ", errors));

                return BadRequest(new ResponseDTO
                {
                    Status = "error",
                    Message = "Registration failed."
                });
            }

            // Optionally add default role(s):
            // await _userManager.AddToRoleAsync(user, "User");

            return StatusCode(201, new ResponseDTO
            {
                Status = "success",
                Message = "Registration successful."
            });
        }

        /// <summary>
        /// Logs in and returns a JWT (TokenDTO).
        /// Returns 200 with TokenDTO, or 401 with ResponseDTO on failure.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // [ApiController] will auto-return 400 for invalid ModelState (DataAnnotations).

            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null)
            {
                // generic message to avoid user enumeration
                await Task.Delay(50);
                return Unauthorized(new ResponseDTO
                {
                    Status = "error",
                    Message = "Invalid username or password."
                });
            }

            // Optional: lockout checks if enabled in Identity options
            // if (await _userManager.IsLockedOutAsync(user)) { ... }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                // Optional: increment failure count for lockout
                await _userManager.AccessFailedAsync(user);

                return Unauthorized(new ResponseDTO
                {
                    Status = "error",
                    Message = "Invalid username or password."
                });
            }

            // Reset failure count after successful login
            await _userManager.ResetAccessFailedCountAsync(user);

            // Issue JWT (TokenDTO)
            var token = await _tokenService.CreateTokenAsync(user);
            return Ok(token);
        }

        /// <summary>
        /// Logs out. With pure Bearer there is nothing to clear server-side.
        /// If you add refresh tokens later, revoke them here.
        /// Returns 200 with ResponseDTO.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok(new ResponseDTO
            {
                Status = "success",
                Message = "Logged out."
            });
        }
    }
}
