using System;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Domain
{
    /// <summary>
    /// Changes:
    /// - [Required] on UserId/Title; length limits for Title/Description.
    /// - Keep CreatedAt default; CompletedAt is nullable and set server-side.
    /// - TimeBlock relation uses SetNull on delete (configured in DbContext).
    /// </summary>
    public class UserTask
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Required]
        public bool IsCompleted { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        // Optional link to a TimeBlock; DbContext sets OnDelete: SetNull
        public int? TimeBlockId { get; set; }

        // Navigations
        public ApplicationUser? User { get; set; }
        public TimeBlock? TimeBlock { get; set; }
    }
}