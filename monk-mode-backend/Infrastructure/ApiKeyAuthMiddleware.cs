using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using monk_mode_backend.Application;

namespace monk_mode_backend.Infrastructure {
    public class ApiKeyAuthMiddleware {
        private readonly RequestDelegate _next;
        private readonly ApiKeyAuthSettings _settings;
        private readonly ILogger<ApiKeyAuthMiddleware> _logger;

        public ApiKeyAuthMiddleware(RequestDelegate next, IOptions<ApiKeyAuthSettings> options, ILogger<ApiKeyAuthMiddleware> logger) {
            _next = next;
            _logger = logger;
            _settings = options.Value ?? new ApiKeyAuthSettings();
        }

        public async Task InvokeAsync(HttpContext context) {
            if (!_settings.Enabled) {
                await _next(context);
                return;
            }

            if (HttpMethods.IsOptions(context.Request.Method)) {
                await _next(context);
                return;
            }

            if (ShouldBypassPath(context.Request.Path)) {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(_settings.Header, out var providedKey) || string.IsNullOrWhiteSpace(providedKey)) {
                _logger.LogWarning("Request blocked: missing API key header {Header} for path {Path}", _settings.Header, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.CompleteAsync();
                return;
            }

            if (!string.Equals(providedKey.ToString(), _settings.Key, StringComparison.Ordinal)) {
                _logger.LogWarning("Request blocked: invalid API key for path {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.CompleteAsync();
                return;
            }

            await _next(context);
        }

        private bool ShouldBypassPath(PathString path) {
            if (_settings.ExcludedPaths == null || _settings.ExcludedPaths.Length == 0) {
                return false;
            }

            return _settings.ExcludedPaths.Any(excluded => !string.IsNullOrWhiteSpace(excluded)
                && path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase));
        }
    }
}
