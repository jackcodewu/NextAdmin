using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using NextAdmin.Log;

namespace NextAdmin.API.Middleware
{
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly long _warningThresholdMs;

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            IConfiguration configuration)
        {
            _next = next;
            _warningThresholdMs = configuration.GetValue<long>("Performance:WarningThresholdMs", 1000);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                if (elapsedMs > _warningThresholdMs)
                {
                    LogHelper.Warn(
                        $"Performance Warning: {context.Request.Method} {context.Request.Path} took {elapsedMs}ms");
                }

                LogHelper.Info(
                    $"Request Finished: {context.Request.Method} {context.Request.Path} took {elapsedMs}ms with status code {context.Response.StatusCode}");
            }
        }
    }
} 
