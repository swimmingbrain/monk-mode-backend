using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Domain
{
    /// <summary>
    /// Changes:
    /// - [Required] on UserId/Date/Title/time fields.
    /// - Title length guard for UI input sanity.
    /// - DbContext enforces EndTime > StartTime and adds useful indexes.
    /// - Tasks remain when a TimeBlock is deleted (SetNull in DbContext).
    /// </summary>
    public class TimeBlock
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public bool IsFocus { get; set; } = false;

        // Navigations
        public ApplicationUser? User { get; set; }
        public ICollection<UserTask> Tasks { get; set; } = new List<UserTask>();
    }
}