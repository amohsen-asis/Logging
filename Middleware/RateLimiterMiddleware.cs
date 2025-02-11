using System.Collections.Concurrent;
using System.Net;

namespace Logging.Middleware
{
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimiterOptions _options;
        private readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();

        public RateLimiterMiddleware(RequestDelegate next, RateLimiterOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (IsRequestAllowed(ipAddress))
            {
                await _next(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
            }
        }

        private bool IsRequestAllowed(string ipAddress)
        {
            var now = DateTime.UtcNow;
            var requests = _requestLog.GetOrAdd(ipAddress, _ => new List<DateTime>());

            // Remove old requests outside the time window
            requests.RemoveAll(time => now - time > _options.TimeWindow);

            if (requests.Count >= _options.MaxRequestsPerTimeWindow)
            {
                return false;
            }

            requests.Add(now);
            return true;
        }
    }

    public class RateLimiterOptions
    {
        public TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(1);
        public int MaxRequestsPerTimeWindow { get; set; } = 100;
    }

    public static class RateLimiterMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiter(
            this IApplicationBuilder builder, 
            Action<RateLimiterOptions>? configureOptions = null)
        {
            var options = new RateLimiterOptions();
            configureOptions?.Invoke(options);

            return builder.UseMiddleware<RateLimiterMiddleware>(options);
        }
    }
}
