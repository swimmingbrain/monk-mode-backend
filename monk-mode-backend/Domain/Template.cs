using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace monk_mode_backend.Domain
{
    /// <summary>
    /// Changes:
    /// - [Required] on Title and UserId.
    /// - CreatedAt defaults to UtcNow for consistency.
    /// - User-scoped ownership (no UserId exposure in DTOs; enforced server-side).
    /// </summary>
    public class Template
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser? User { get; set; }
        public ICollection<TemplateBlock> TemplateBlocks { get; set; } = new List<TemplateBlock>();
    }
}