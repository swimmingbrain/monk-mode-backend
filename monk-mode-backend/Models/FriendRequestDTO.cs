using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace monk_mode_backend.Models
{
    /// <summary>
    /// Request DTO for sending a friend request.
    /// Changes:
    /// - Clear whitelist input (only one identifier allowed).
    /// - Validation ensures exactly one field is provided (username OR id).
    /// - Error message simplified and translated to English for app consistency.
    /// </summary>
    public class FriendRequestDTO : IValidatableObject
    {
        /// <summary>
        /// Preferred: target user's username.
        /// </summary>
        [StringLength(64)]
        public string? FriendUsername { get; set; }

        /// <summary>
        /// Alternative: target user's ID.
        /// </summary>
        public string? FriendId { get; set; }

        /// <summary>
        /// Validation logic — ensures that exactly one of the two fields is filled.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasUsername = !string.IsNullOrWhiteSpace(FriendUsername);
            var hasId = !string.IsNullOrWhiteSpace(FriendId);

            if (hasUsername == hasId) // both true or both false
            {
                yield return new ValidationResult(
                    "Please provide either a username or an ID — not both.",
                    new[] { nameof(FriendUsername), nameof(FriendId) }
                );
            }
        }
    }
}
