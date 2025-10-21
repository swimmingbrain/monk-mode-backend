using System;

namespace monk_mode_backend.DTOs
{
    public class UserProfileDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Xp { get; set; }
        public int Level { get; set; }
    }
}
