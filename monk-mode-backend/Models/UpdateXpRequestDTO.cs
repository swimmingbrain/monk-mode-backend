using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.DTOs
{
    public class UpdateXpRequestDTO
    {
        // Begrenze die Änderung serverseitig; Range anpassbar nach Bedarf
        [Range(1, 1000)]
        public int XpToAdd { get; set; }
    }
}
