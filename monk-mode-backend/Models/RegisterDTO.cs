using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Models
{
    /// <summary>
    /// Request DTO for user registration.
    /// Changes:
    /// - Added EmailAddress and StringLength constraints for early validation.
    /// </summary>
    public class RegisterDTO
    {
        // kept: original Required message
        [Required(ErrorMessage = "User Name is required")]
        // new: explicit length guard with a friendly message
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Username must be 3–32 characters")]
        public string Username { get; set; } = string.Empty;

        // kept: original Required message
        [Required(ErrorMessage = "Email is required")]
        // new: email format validation with a clear message
        [EmailAddress(ErrorMessage = "Email format is invalid")]
        [StringLength(255, ErrorMessage = "Email must be at most 255 characters")]
        public string Email { get; set; } = string.Empty;

        // kept: original Required message
        [Required(ErrorMessage = "Password is required")]
        // new: mirrors your Identity password policy at a high level
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
        public string Password { get; set; } = string.Empty;
    }
}
