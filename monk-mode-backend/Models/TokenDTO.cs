using System;
using System.Collections.Generic;

namespace monk_mode_backend.DTOs
{
    public class TokenDTO
    {
        public string Token { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
        public DateTime Expiration { get; init; }
        public IList<string> Roles { get; init; } = new List<string>();
    }
}
