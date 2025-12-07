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
                // 验证请求体大小
                if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Code = "413",
                        Message = "请求体过大",
                        Data = (object?)null
                    });
                    return;
                }

                // 验证请求方法
                if (!IsValidHttpMethod(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Code = "405",
                        Message = "不支持的HTTP方法",
                        Data = (object?)null
                    });
                    return;
                }

                // 验证Content-Type
                if (context.Request.Method != "GET" && !IsValidContentType(context.Request.ContentType))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Code = "415",
                        Message = "不支持的Content-Type",
                        Data = (object?)null
                    });
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "请求验证中间件发生错误");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    Code = "500",
                    Message = "服务器内部错误",
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
