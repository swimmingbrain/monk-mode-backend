using System.Collections.Generic;
using System.Threading.Tasks;
using monk_mode_backend.Models;

namespace monk_mode_backend.Application
{
    /// <summary>
    /// IFriendshipService (updated):
    /// - List methods now return IEnumerable<FriendshipResponseDTO> so the UI gets FriendUsername.
    /// - SendFriendRequestAsync now takes friendUsername (not friendId); service resolves the user internally.
    /// - Accept/Reject/Remove signatures unchanged; return types aligned with ResponseDTO-less service design.
    /// Security notes:
    /// - Service throws InvalidOperationException on invalid actions; controllers should map to ResponseDTO.
    /// </summary>
    public interface IFriendshipService
    {
        Task<IEnumerable<FriendshipResponseDTO>> GetFriendshipsAsync(string userId);
        Task<IEnumerable<FriendshipResponseDTO>> GetFriendRequestsAsync(string userId);
        Task<IEnumerable<FriendshipResponseDTO>> GetSentFriendRequestsAsync(string userId);

        Task<FriendshipResponseDTO> SendFriendRequestAsync(string userId, string friendUsername);
        Task<FriendshipResponseDTO> AcceptFriendRequestAsync(string userId, int friendshipId);
        Task<FriendshipResponseDTO> RejectFriendRequestAsync(string userId, int friendshipId);

        Task<bool> RemoveFriendAsync(string userId, int friendshipId);
    }
}
