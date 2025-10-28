using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using monk_mode_backend.Domain;
using monk_mode_backend.Models;
using monk_mode_backend.Application;

namespace monk_mode_backend.Application
{
    /// <summary>
    /// JWTService – hardened per security feedback (no refresh logic here).
    /// - Validates config (Secret/Issuer/Audience)
    /// - Uses UTC times and short-lived access tokens (configurable)
    /// - Minimal explicit claims (sub, jti, optional name/email, roles)
    /// - Implements ITokenService (both CreateTokenAsync overloads + GetPrincipalFromToken)
    /// </summary>
    public class JWTService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public JWTService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        /// <summary>
        /// Backward-compatible default: creates a JWT with defaults
        /// from configuration (Issuer/Audience/Lifetime).
        /// </summary>
        public async Task<TokenDTO> CreateTokenAsync(ApplicationUser user)
        {
            return await CreateTokenAsync(user, extraClaims: null, lifetime: null, audience: null);
        }

        /// <summary>
        /// Advanced variant: allows optional overrides for extra claims, lifetime and audience.
        /// </summary>
        public async Task<TokenDTO> CreateTokenAsync(
            ApplicationUser user,
            IReadOnlyDictionary<string, string>? extraClaims = null,
            TimeSpan? lifetime = null,
            string? audience = null)
        {
            // --- Load and validate JWT settings ---
            var secret = _configuration["JwtSettings:Secret"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var confAud = _configuration["JwtSettings:Audience"];
            var minutes = _configuration["JwtSettings:AccessTokenMinutes"];

            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT Secret is not configured (JwtSettings:Secret).");
            if (string.IsNullOrWhiteSpace(issuer))
                throw new InvalidOperationException("JWT Issuer is not configured (JwtSettings:Issuer).");
            if (string.IsNullOrWhiteSpace(confAud))
                throw new InvalidOperationException("JWT Audience is not configured (JwtSettings:Audience).");

            // Lifetime: explicit override > config > default(60)
            int cfgMinutes = 60;
            if (int.TryParse(minutes, out var parsed) && parsed > 0)
                cfgMinutes = parsed;

            var effectiveLifetime = lifetime ?? TimeSpan.FromMinutes(cfgMinutes);
            var effectiveAudience = audience ?? confAud;

            // --- Build minimal claims set ---
            var claims = new List<Claim>
            {
                // Subject = stable user id
                new(JwtRegisteredClaimNames.Sub, user.Id),
                // Unique token id
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Optional convenience claims (remove if not needed)
            if (!string.IsNullOrWhiteSpace(user.UserName))
                claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            // Roles
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Optional extra claims (least privilege: only exact matches)
            if (extraClaims is not null)
            {
                foreach (var kv in extraClaims)
                {
                    if (!string.IsNullOrWhiteSpace(kv.Key) && kv.Value is not null)
                    {
                        claims.Add(new Claim(kv.Key, kv.Value));
                    }
                }
            }

            // --- Sign and create token ---
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresUtc = DateTime.UtcNow.Add(effectiveLifetime);

            var jwtToken = new JwtSecurityToken(
                issuer: issuer,
                audience: effectiveAudience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresUtc,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            // Map to your TokenDTO (PascalCase)
            return new TokenDTO
            {
                Token = tokenString,
                Expiration = jwtToken.ValidTo, // UTC
                Roles = roles,
                Id = user.Id
            };
        }

        /// <summary>
        /// Validates a token and returns a ClaimsPrincipal.
        /// Set validateLifetime=false to parse expired tokens (diagnostics).
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true)
        {
            var secret = _configuration["JwtSettings:Secret"];
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];

            if (string.IsNullOrWhiteSpace(secret) ||
                string.IsNullOrWhiteSpace(issuer) ||
                string.IsNullOrWhiteSpace(audience))
            {
                return null;
            }

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));

            var parms = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                // Optionally allow expired tokens for parsing when requested
                ValidateLifetime = validateLifetime,
                // keep small skew; pipeline can set its own (e.g., 2 minutes)
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var principal = handler.ValidateToken(token, parms, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
