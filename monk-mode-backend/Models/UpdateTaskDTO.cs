using System;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Models
{
    public class UpdateTaskDTO
    {
        /// <summary>
        /// Request-DTO (Whitelist) zum Aktualisieren einer Task.
        /// Änderungen:
        /// - [Required]/[StringLength] ergänzt.
        /// - Keine kritischen Felder (Id/UserId/CreatedAt etc.) → Overposting-Schutz.
        /// </summary>

        [Required, StringLength(160)]
        public string Title { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public bool IsCompleted { get; set; }
    }
}
