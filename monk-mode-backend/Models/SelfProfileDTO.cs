using monk_mode_backend.Models;
using System;

namespace monk_mode_backend.Models
{
    public class SelfProfileDTO
    {

        /// summary
        /// DTO für das eigene Benutzerprofil (Self View).
        /// Wird ausschließlich für den eingeloggten Benutzer verwendet,
        /// z. B. im Endpoint /api/user/me.
        /// Enthält sensible Daten wie E-Mail und interne ID.
        


        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Xp { get; set; }
        public int Level { get; set; }
    }
}

