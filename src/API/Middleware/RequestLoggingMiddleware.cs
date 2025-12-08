using Microsoft.AspNetCore.Http;
using System.Text;
using NextAdmin.Log;

namespace NextAdmin.API.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Log request information
                var request = context.Request;
                var requestBody = await GetRequestBodyAsync(request);
                
                LogHelper.Info(
                    $"Request Start: {request.Method} {request.Path}{request.QueryString} | Body: {requestBody}");

                // Call next middleware
                await _next(context);

                // Log response information
                LogHelper.Info(
                    $"Request End: {request.Method} {request.Path} | StatusCode: {context.Response.StatusCode}");
            }
            catch (Exception ex)
            {
                LogHelper.Error("Error in RequestLoggingMiddleware.", ex);
                throw;
            }
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            if (request.Body == null)
                return string.Empty;

            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength ?? 0)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0;

            return bodyAsText;
        }
    }
} 
