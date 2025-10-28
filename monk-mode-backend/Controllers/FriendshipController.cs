using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using monk_mode_backend.Domain;     // ApplicationUser
using monk_mode_backend.Application; // IFriendshipService
using monk_mode_backend.Models;     // ResponseDTO, FriendRequestDTO, FriendshipDTO (=> Service returns these)

namespace monk_mode_backend.Controllers
{
    /// <summary>
    /// Changes (fixed to match your actual models & service):
    /// - Uses ONLY FriendRequestDTO.FriendId (no FriendUsername access).
    /// - List endpoints return IEnumerable<FriendshipDTO> (as your service does),
    ///   removing the invalid cast to FriendshipResponseDTO.
    /// - Thin controller: delegates to IFriendshipService; consistent ResponseDTO on errors.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FriendshipController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FriendshipController> _logger;

        public FriendshipController(
            IFriendshipService friendshipService,
            UserManager<ApplicationUser> userManager,
            ILogger<FriendshipController> logger)
        {
            _friendshipService = friendshipService;
            _userManager = userManager;
            _logger = logger;
        }

        // ------------------------
        // Create / Mutate
        // ------------------------

        /// <summary>
        /// Send a friend request to another user (by ID).
        /// NOTE: Adjusted to your current FriendRequestDTO, which exposes only FriendId.
        /// </summary>
        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] FriendRequestDTO dto)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            // CHANGED: Only FriendId is used (FriendUsername removed)
            var friendId = dto.FriendId;
            if (string.IsNullOrWhiteSpace(friendId))
            {
                return BadRequest(new ResponseDTO
                {
                    Status = "error",
                    Message = "FriendId is required."
                });
            }

            if (string.Equals(friendId, me.Id, StringComparison.Ordinal))
            {
                return BadRequest(new ResponseDTO
                {
                    Status = "error",
                    Message = "You cannot send a friend request to yourself."
                });
            }

            try
            {
                // Service returns a DTO (your implementation). We just pass it through.
                var created = await _friendshipService.SendFriendRequestAsync(me.Id, friendId);
                // If your service indicates duplicate/not-found via exceptions or null,
                // handle that here accordingly. For now, assume happy path returns an object.
                return StatusCode(201, created);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseDTO { Status = "error", Message = "User not found." });
            }
            catch (InvalidOperationException)
            {
                return Conflict(new ResponseDTO { Status = "error", Message = "A friendship or request already exists." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SendFriendRequestAsync failed for {UserId} -> {FriendId}", me.Id, friendId);
                return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to send friend request." });
            }
        }

        /// <summary>
        /// Accept a friend request (current user must be the recipient).
        /// </summary>
        [HttpPost("{id:int}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            try
            {
                var updated = await _friendshipService.AcceptFriendRequestAsync(me.Id, id);
                return Ok(updated); // Pass through whatever DTO your service returns
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseDTO { Status = "error", Message = "Friend request not found." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new ResponseDTO { Status = "error", Message = "Request is not pending." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AcceptFriendRequestAsync failed for {UserId} on request {RequestId}", me.Id, id);
                return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to accept friend request." });
            }
        }

        /// <summary>
        /// Reject a friend request (current user must be the recipient).
        /// </summary>
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            try
            {
                var updated = await _friendshipService.RejectFriendRequestAsync(me.Id, id);
                return Ok(updated); // Pass through service DTO
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseDTO { Status = "error", Message = "Friend request not found." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new ResponseDTO { Status = "error", Message = "Request is not pending." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RejectFriendRequestAsync failed for {UserId} on request {RequestId}", me.Id, id);
                return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to reject friend request." });
            }
        }

        /// <summary>
        /// Remove an accepted friendship (sender or recipient can remove).
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Remove(int id)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            try
            {
                var removed = await _friendshipService.RemoveFriendAsync(me.Id, id);
                if (!removed)
                    return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to remove friendship." });

                return Ok(new ResponseDTO { Status = "success", Message = "Friendship removed." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ResponseDTO { Status = "error", Message = "Friendship not found." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RemoveFriendAsync failed for {UserId} on friendship {FriendshipId}", me.Id, id);
                return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to remove friendship." });
            }
        }

        // ------------------------
        // Read (CHANGED: return IEnumerable<FriendshipDTO>)
        // ------------------------

        /// <summary>
        /// Get your accepted friends (both directions).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FriendshipDTO>>> GetFriends()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            try
            {
                var list = await _friendshipService.GetFriendshipsAsync(me.Id);
                return Ok(list); // IEnumerable<FriendshipDTO>
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetFriendshipsAsync failed for {UserId}", me.Id);
                return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to load friendships." });
            }
        }

        /// <summary>
        /// Get incoming pending friend requests (requests sent to you).
        /// </summary>
        [HttpGet("requests/incoming")]
        public async Task<ActionResult<IEnumerable<FriendshipDTO>>> GetIncoming()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            try
            {
                var list = await _friendshipService.GetFriendRequestsAsync(me.Id);
                return Ok(list); // IEnumerable<FriendshipDTO>
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetFriendRequestsAsync failed for {UserId}", me.Id);
                return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to load incoming requests." });
            }
        }

        /// <summary>
        /// Get outgoing pending friend requests (requests you sent).
        /// </summary>
        [HttpGet("requests/outgoing")]
        public async Task<ActionResult<IEnumerable<FriendshipDTO>>> GetOutgoing()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me is null)
                return Unauthorized(new ResponseDTO { Status = "error", Message = "Unauthorized." });

            try
            {
                var list = await _friendshipService.GetSentFriendRequestsAsync(me.Id);
                return Ok(list); // IEnumerable<FriendshipDTO>
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetSentFriendRequestsAsync failed for {UserId}", me.Id);
                return BadRequest(new ResponseDTO { Status = "error", Message = "Unable to load outgoing requests." });
            }
        }
    }
}
