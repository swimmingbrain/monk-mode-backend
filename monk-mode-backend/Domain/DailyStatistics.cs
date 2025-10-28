using System;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Domain
{
    /// <summary>
    /// Changes:
    /// - Marked key fields as [Required].
    /// - Kept Date as DateTime (server enforces "date-only" semantics via DbContext/index).
    /// - DbContext adds a unique index on (UserId, Date).
    /// </summary>
    public class DailyStatistics
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int TotalFocusTime { get; set; } = 0;

        [Required]
        public int Xp { get; set; } = 0;

        [Required]
        public int Level { get; set; } = 1;

        // Navigation
        public ApplicationUser? User { get; set; }
    }
}