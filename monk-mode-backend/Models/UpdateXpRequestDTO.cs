using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Models {
    public class UpdateXpRequestDTO {
        [Range(1, 100000000, ErrorMessage = "XpToAdd must be between 1 and 10000.")]
        public int XpToAdd { get; set; }
    }
}
