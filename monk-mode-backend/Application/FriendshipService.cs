using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;
using monk_mode_backend.Hubs;
using monk_mode_backend.Infrastructure;
using monk_mode_backend.Models;

namespace monk_mode_backend.Application
{
    /// <summary>
    /// FriendshipService – aligned to FriendshipDTO/FriendshipResponseDTO:
    /// - No writes to non-existing properties (e.g., no .Message).
    /// - Lists return FriendshipResponseDTO (includes FriendUsername for UI).
    /// - Errors are thrown as exceptions; controllers map them to ResponseDTO.
    /// - Defensive null/ownership checks.
    /// </summary>
    public class FriendshipService : IFriendshipService
    {
        private readonly MonkModeDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public FriendshipService(
            MonkModeDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IMapper mapper,
            IHubContext<NotificationHub> hubContext)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _mapper = mapper;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Returns accepted friendships for userId (UI-enriched).
        /// </summary>
        public async Task<IEnumerable<FriendshipResponseDTO>> GetFriendshipsAsync(string userId)
        {
            var friendships = await _dbContext.Friendships
                .Where(f => (f.UserId == userId || f.FriendId == userId) && f.Status == "Accepted")
                .AsNoTracking()
                .ToListAsync();

            var results = new List<FriendshipResponseDTO>();

            foreach (var friendship in friendships)
            {
                var otherId = friendship.UserId == userId ? friendship.FriendId : friendship.UserId;
                var other = await _userManager.FindByIdAsync(otherId);
                if (other == null) continue;

                var dto = _mapper.Map<FriendshipResponseDTO>(friendship);
                dto.FriendUsername = other.UserName ?? "Unknown";
                // dto.Status is already mapped from entity ("Accepted")
                results.Add(dto);
            }

            return results;
        }

        /// <summary>
        /// Returns incoming pending friend requests (received by userId).
        /// </summary>
        public async Task<IEnumerable<FriendshipResponseDTO>> GetFriendRequestsAsync(string userId)
        {
            var requests = await _dbContext.Friendships
                .Where(f => f.FriendId == userId && f.Status == "Pending")
                .AsNoTracking()
                .ToListAsync();

            var results = new List<FriendshipResponseDTO>();

            foreach (var req in requests)
            {
                var requester = await _userManager.FindByIdAsync(req.UserId);
                if (requester == null) continue;

                var dto = _mapper.Map<FriendshipResponseDTO>(req);
                dto.FriendUsername = requester.UserName ?? "Unknown";
                // dto.Status = "Pending"
                results.Add(dto);
            }
            return results;
        }

        /// <summary>
        /// Returns outgoing pending friend requests (sent by userId).
        /// </summary>
        public async Task<IEnumerable<FriendshipResponseDTO>> GetSentFriendRequestsAsync(string userId)
        {
            var sent = await _dbContext.Friendships
                .Where(f => f.UserId == userId && f.Status == "Pending")
                .AsNoTracking()
                .ToListAsync();

            var results = new List<FriendshipResponseDTO>();
            foreach (var s in sent)
            {
                var recipient = await _userManager.FindByIdAsync(s.FriendId);
                if (recipient == null) continue;

                var dto = _mapper.Map<FriendshipResponseDTO>(s);
                dto.FriendUsername = recipient.UserName ?? "Unknown";
                // dto.Status = "Pending"
                results.Add(dto);
            }

            return results;
        }

        /// <summary>
        /// Creates a pending friend request from userId to friendUsername.
        /// Throws if invalid (user not found, already friends/request exists, self-add).
        /// Returns created request as FriendshipResponseDTO.
        /// </summary>
        public async Task<FriendshipResponseDTO> SendFriendRequestAsync(string userId, string friendUsername)
        {
            var friend = await _userManager.FindByNameAsync(friendUsername);
            if (friend == null)
                throw new InvalidOperationException("User not found.");

            var friendId = friend.Id;

            if (userId == friendId)
                throw new InvalidOperationException("You cannot add yourself.");

            var existing = await _dbContext.Friendships.FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId));

            if (existing != null)
            {
                if (existing.Status == "Accepted")
                    throw new InvalidOperationException("You are already friends.");
                throw new InvalidOperationException("A friend request already exists.");
            }

            var friendship = new Friendship
            {
                UserId = userId,
                FriendId = friendId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Friendships.Add(friendship);
            await _dbContext.SaveChangesAsync();

            var requester = await _userManager.FindByIdAsync(userId);
            await _hubContext.Clients.User(friendId)
                .SendAsync("ReceiveFriendRequest", userId, requester?.UserName ?? "Unknown");

            var dto = _mapper.Map<FriendshipResponseDTO>(friendship);
            dto.FriendUsername = friend.UserName ?? "Unknown";
            return dto;
        }

        /// <summary>
        /// Accepts a pending friend request addressed to userId.
        /// Throws if not found/unauthorized/already handled.
        /// Returns updated friendship as FriendshipResponseDTO.
        /// </summary>
        public async Task<FriendshipResponseDTO> AcceptFriendRequestAsync(string userId, int friendshipId)
        {
            var friendship = await _dbContext.Friendships.FindAsync(friendshipId);
            if (friendship == null)
                throw new InvalidOperationException("Friend request not found.");

            if (friendship.FriendId != userId)
                throw new InvalidOperationException("Unauthorized to accept this request.");

            if (friendship.Status != "Pending")
                throw new InvalidOperationException("Request already handled.");

            friendship.Status = "Accepted";
            _dbContext.Friendships.Update(friendship);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.User(friendship.UserId)
                .SendAsync("FriendRequestAccepted", userId);

            var requester = await _userManager.FindByIdAsync(friendship.UserId);
            var dto = _mapper.Map<FriendshipResponseDTO>(friendship);
            dto.FriendUsername = requester?.UserName ?? "Unknown";
            return dto;
        }

        /// <summary>
        /// Rejects a pending friend request addressed to userId.
        /// Throws if not found/unauthorized/already handled.
        /// Returns the removed friendship as a DTO snapshot (optional).
        /// </summary>
        public async Task<FriendshipResponseDTO> RejectFriendRequestAsync(string userId, int friendshipId)
        {
            var friendship = await _dbContext.Friendships.FindAsync(friendshipId);
            if (friendship == null)
                throw new InvalidOperationException("Friend request not found.");

            if (friendship.FriendId != userId)
                throw new InvalidOperationException("Unauthorized to reject this request.");

            if (friendship.Status != "Pending")
                throw new InvalidOperationException("Request already handled.");

            var snapshot = _mapper.Map<FriendshipResponseDTO>(friendship);
            var requester = await _userManager.FindByIdAsync(friendship.UserId);
            snapshot.FriendUsername = requester?.UserName ?? "Unknown";

            _dbContext.Friendships.Remove(friendship);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.User(friendship.UserId)
                .SendAsync("FriendRequestRejected", userId);

            return snapshot;
        }

        /// <summary>
        /// Removes an accepted friendship if userId is participant.
        /// Returns true if removed; false if not found/unauthorized/wrong status.
        /// </summary>
        public async Task<bool> RemoveFriendAsync(string userId, int friendshipId)
        {
            var friendship = await _dbContext.Friendships.FindAsync(friendshipId);
            if (friendship == null ||
                (friendship.UserId != userId && friendship.FriendId != userId) ||
                friendship.Status != "Accepted")
                return false;

            _dbContext.Friendships.Remove(friendship);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
