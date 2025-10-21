    using System;

    namespace monk_mode_backend.Models
    {
        /// summary
        /// DTO für öffentliche Benutzerprofile (Public View).
        /// Wird für andere Nutzer sichtbar, z. B. in Freundeslisten,
        /// Leaderboards oder bei Freundschaftsanfragen.
        /// Enthält keine sensiblen Informationen wie E-Mail oder ID.

        public class PublicProfileDTO
        {
            public string Username { get; set; } = string.Empty;
            public int Xp { get; set; }
            public int Level { get; set; }
        }
    }

