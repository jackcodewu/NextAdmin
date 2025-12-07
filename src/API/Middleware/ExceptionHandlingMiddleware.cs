using Microsoft.AspNetCore.Http;
using System.Net;
using NextAdmin.Log;

namespace NextAdmin.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                LogHelper.Error("An unhandled exception has occurred.", ex);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                Code = "500",
                Message = "服务器内部错误",
                Data = (object?)null
            };

            switch (exception)
            {
                case ArgumentException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response = new
                    {
                        Code = "400",
                        Message = "请求参数错误",
                        Data = (object?)null
                    };
                    break;
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new
                    {
                        Code = "401",
                        Message = "未授权访问",
                        Data = (object?)null
                    };
                    break;
                case NotImplementedException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    response = new
                    {
                        Code = "501",
                        Message = "功能未实现",
                        Data = (object?)null
                    };
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            await context.Response.WriteAsJsonAsync(response);
        }
    }
} 
