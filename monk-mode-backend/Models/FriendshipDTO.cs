using System;

namespace monk_mode_backend.Models
{
    /// <summary>
    /// Response-DTO für eine Freundschafts-Beziehung (read-only).
    /// Änderungen:
    /// - Klar als Response-only markiert (keine Input-Nutzung).
    /// - Einheitliche PascalCase-Properties.
    /// </summary>
    public class FriendshipDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FriendId { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // z.B. Pending/Accepted/Blocked
        public DateTime CreatedAt { get; set; }
    }
}
