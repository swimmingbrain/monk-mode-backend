// Application/ITokenService.cs
using System.Security.Claims;
using monk_mode_backend.Domain;
using monk_mode_backend.Models;

namespace monk_mode_backend.Application
{
    /// <summary>
    /// Issues short-lived JWT access tokens.
    /// NOTE: This interface is refresh-token agnostic on purpose (handled later).
    /// Security goals:
    /// - Centralize claim creation and signing.
    /// - Allow environment-specific lifetimes (e.g., via optional overrides).
    /// - Keep a backward-compatible CreateTokenAsync(ApplicationUser) entry point.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Backward-compatible default: creates a JWT for the given user
        /// with the service's default claims, issuer/audience, and lifetime
        /// (typically from configuration / environment).
        /// </summary>
        Task<TokenDTO> CreateTokenAsync(ApplicationUser user);

        /// <summary>
        /// Advanced creation with optional overrides, to support security policies without
        /// changing the service implementation:
        /// - extraClaims: add minimal, explicit claims (least privilege).
        /// - lifetime: override default access token lifetime (e.g., shorter in prod).
        /// - audience: override audience if you separate mobile/web backends.
        /// </summary>
        Task<TokenDTO> CreateTokenAsync(
            ApplicationUser user,
            IReadOnlyDictionary<string, string>? extraClaims = null,
            TimeSpan? lifetime = null,
            string? audience = null);

        /// <summary>
        /// Validates a token and returns a principal.
        /// Useful for diagnostics/health checks or internal flows where you need to
        /// parse claims without relying on the ASP.NET pipeline.
        /// Set validateLifetime=false for controlled scenarios (e.g., debugging).
        /// </summary>
        ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = true);
    }
}
