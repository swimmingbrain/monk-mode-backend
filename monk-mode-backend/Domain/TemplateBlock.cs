using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace monk_mode_backend.Domain
{
    public class TemplateBlock
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TemplateId { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public bool IsFocus { get; set; }

        // Navigation property
        [ForeignKey("TemplateId")]
        public Template Template { get; set; }
    }
} 