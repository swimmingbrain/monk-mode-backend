using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Domain
{
    /// <summary>
    /// Changes:
    /// - Initialize navigation collections to empty lists (no null checks needed in mappers/JSON).
    /// - Keep Identity inheritance; store only app-specific fields here.
    /// - Defaults for CreatedAt/Xp/Level for consistent initial state.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public int Level { get; set; } = 1;

        [Required]
        public int Xp { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Initialized navigation collections (avoid nulls in mappings/serialization)
        public ICollection<UserTask> Tasks { get; set; } = new List<UserTask>();
        public ICollection<TimeBlock> TimeBlocks { get; set; } = new List<TimeBlock>();
        public ICollection<Template> Templates { get; set; } = new List<Template>();
        public ICollection<DailyStatistics> DailyStatistics { get; set; } = new List<DailyStatistics>();
    }
}
