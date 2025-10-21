using System;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.DTOs
{
    public class UpdateTaskDTO
    {
        [Required, StringLength(160)]
        public string Title { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public bool IsCompleted { get; set; }
    }
}
