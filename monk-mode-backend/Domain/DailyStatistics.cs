using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace monk_mode_backend.Domain
{
    public class DailyStatistics
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int TotalFocusTime { get; set; } // in seconds

        // Navigation property
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
} 