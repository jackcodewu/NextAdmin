using System.Text.Json;
using NextAdmin.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NextAdmin.API.Middleware
{
    public class RequestValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Validate request body size
                if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Code = "413",
                        Message = "Request body too large",
                        Data = (object?)null
                    });
                    return;
                }

                // Validate request method
                if (!IsValidHttpMethod(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Code = "405",
                        Message = "Unsupported HTTP method",
                        Data = (object?)null
                    });
                    return;
                }

                // Validate Content-Type
                if (context.Request.Method != "GET" && !IsValidContentType(context.Request.ContentType))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Code = "415",
                        Message = "Unsupported Content-Type",
                        Data = (object?)null
                    });
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Error occurred in request validation middleware");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    Code = "500",
                    Message = "Internal server error",
                    Data = (object?)null
                });
            }
        }

        private bool IsValidHttpMethod(string method)
        {
            return method == "GET" || method == "POST" || method == "PUT" || 
                   method == "DELETE" || method == "PATCH" || method == "HEAD" || 
                   method == "OPTIONS";
        }

        private bool IsValidContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            return contentType.Contains("application/json") || 
                   contentType.Contains("application/x-www-form-urlencoded") ||
                   contentType.Contains("multipart/form-data");
        }
    }
} 
