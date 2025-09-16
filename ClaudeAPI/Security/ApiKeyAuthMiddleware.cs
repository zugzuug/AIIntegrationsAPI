using System;
using System.Linq;
using System.Threading.Tasks;
using AIIntegrationsAPI.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIIntegrationsAPI.Security
{
    /// <summary>Simple API key authentication middleware. Accepts OwnerKey (no expiry) or any non-expired Guest key. Bypasses Swagger, health, and preflight requests so the UI and probes work without a key.</summary>
    public sealed class ApiKeyAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthMiddleware> _logger;
        private readonly ApiKeyOptions _opts;

        /// <summary>Initializes a new instance of the ApiKeyAuthMiddleware class.</summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="options">API key options configuration.</param>
        /// <param name="logger">Logger for diagnostics.</param>
        public ApiKeyAuthMiddleware(
            RequestDelegate next,
            IOptions<ApiKeyOptions> options,
            ILogger<ApiKeyAuthMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Handles API key authentication for incoming HTTP requests.</summary>
        /// <param name="context">The current HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            // 1) Always allow CORS preflight and docs/health endpoints
            if (context.Request.Method == HttpMethods.Options ||
                path.StartsWith("/swagger") ||
                path.StartsWith("/healthz") ||
                path == "/" ||
                path.StartsWith("/index.html") ||
                path.StartsWith("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            // 2) Validate API key header
            var headerName = string.IsNullOrWhiteSpace(_opts.HeaderName) ? "x-api-key" : _opts.HeaderName;

            if (!context.Request.Headers.TryGetValue(headerName, out var provided) ||
                string.IsNullOrWhiteSpace(provided))
            {
                _logger.LogWarning("Missing API key. Header '{HeaderName}' was not provided. Path={Path}",
                    headerName, path);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            var key = provided.ToString();

            // 3) Owner key (never expires)
            if (!string.IsNullOrEmpty(_opts.OwnerKey) && key == _opts.OwnerKey)
            {
                await _next(context);
                // OWNER accepted
                _logger.LogInformation("API auth OK {KeyType} Path={Path}", "owner", context.Request.Path);
                return;
            }

            // 4) Guest keys (must exist and be unexpired)
            var nowUtc = DateTime.UtcNow;
            var match = _opts.Guests.FirstOrDefault(g => g.Key == key);

            if (match is not null && match.ExpiresUtc > nowUtc)
            {
                await _next(context);
                // GUEST accepted
                _logger.LogInformation("API auth OK {KeyType} Path={Path} GuestLabel={Label} ExpiresUtc={ExpiresUtc:o}",
                    "guest", context.Request.Path, match.Label, match.ExpiresUtc);

                return;
            }

            // FAIL: missing, expired, or invalid key
            _logger.LogWarning("API auth FAIL Path={Path} Reason={Reason}", context.Request.Path, "missing/expired/invalid");

            _logger.LogWarning(
                "API key rejected. Header={HeaderName}, Provided={Provided}, NowUtc={Now}, Path={Path}",
                headerName, key, nowUtc, path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
        }
    }
}
