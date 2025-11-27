using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using monk_mode_backend.Domain;
using monk_mode_backend.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace monk_mode_backend.Application {
    public class JWTService : ITokenService {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly JwtSettings jwtSettings;

        public JWTService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtOptions) {
            this.userManager = userManager;
            this.jwtSettings = jwtOptions.Value;
        }

        public async Task<TokenDTO> CreateTokenAsync(ApplicationUser user) {

            if (string.IsNullOrWhiteSpace(jwtSettings.Secret)) {
                throw new InvalidOperationException("JwtSettings:Secret is not configured.");
            }
            if (string.IsNullOrWhiteSpace(jwtSettings.Issuer)) {
                throw new InvalidOperationException("JwtSettings:Issuer is not configured.");
            }
            if (string.IsNullOrWhiteSpace(jwtSettings.Audience)) {
                throw new InvalidOperationException("JwtSettings:Audience is not configured.");
            }

            var userRoles = await userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };


            foreach (string userRole in userRoles) {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }


            var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret));
            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                expires: DateTime.UtcNow.AddDays(15),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );
            return new TokenDTO() {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                roles = userRoles,
                id = user.Id
            };
        }
    }
}
