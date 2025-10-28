using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace monk_mode_backend.Domain
{
    /// <summary>
    /// Changes:
    /// - [Required] annotations for FK and core fields.
    /// - Title length guard.
    /// - Time constraints (End > Start) enforced via DbContext check constraint.
    /// </summary>
    public class TemplateBlock
    {
        public int Id { get; set; }

        [Required]
        public int TemplateId { get; set; }

        [Required, StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public bool IsFocus { get; set; } = false;

        // Navigation
        public Template? Template { get; set; }
    }
}