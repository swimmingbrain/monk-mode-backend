using System;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Domain
{
    /// <summary>
    /// Changes:
    /// - [Required] on identifiers and Status.
    /// - Limited Status length (DbContext may later convert to enum/string).
    /// - CreatedAt defaults to UtcNow.
    /// - DbContext adds unique index on (UserId, FriendId) and restrict delete.
    /// </summary>
    public class Friendship
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string FriendId { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending"; // e.g., Pending/Accepted/Declined/Blocked

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigations (optional)
        public ApplicationUser? User { get; set; }
    }
}
