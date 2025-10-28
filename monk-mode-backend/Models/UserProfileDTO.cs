using System;

namespace monk_mode_backend.DTOs
{
    public class UserProfileDTO
    {
        /// <summary>
        /// Response-DTO fürs eigene Benutzerprofil (Self-View).
        /// Änderungen:
        /// - Klar als Self-Profile gedacht (enthält E-Mail & Id).
        /// - Für öffentliche Sicht zusätzlich PublicProfileDTO verwenden.
        /// </summary>

        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Xp { get; set; }
        public int Level { get; set; }
    }
}
