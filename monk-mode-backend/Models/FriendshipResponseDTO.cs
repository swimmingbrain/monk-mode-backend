using System;

namespace monk_mode_backend.Models
{
    /// <summary>
    /// Angereichertes Response-DTO für Freundschaften (read-only),
    /// z. B. für Listenansichten im Frontend.
    /// Änderungen:
    /// - Enthält zusätzlich FriendUsername für UI-Darstellung.
    /// - Keine Input-Nutzung (nur Server → Client).
    /// </summary>
    public class FriendshipResponseDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FriendId { get; set; } = string.Empty;

        // Optionaler Komfort für die UI:
        public string FriendUsername { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
