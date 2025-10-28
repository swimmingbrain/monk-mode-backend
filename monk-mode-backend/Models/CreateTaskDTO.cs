using System;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Models
{
    /// summary
    /// Request-DTO (Whitelist) zum Erstellen einer Task.
    /// Änderungen:
    /// - [Required]/[StringLength] ergänzt → frühe Validierung.
    /// - Nur erlaubte Felder, kein UserId/Id/CreatedAt → Overposting-Schutz.

    public class CreateTaskDTO
    {
        [Required, StringLength(160)]
        public string Title { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }
    }
}
