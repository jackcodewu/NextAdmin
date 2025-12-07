using NextAdmin.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text;

namespace NextAdmin.API.Middleware
{
    public class CacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly int _defaultCacheDuration;

        public CacheMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _next = next;
            _cache = cache;
            _defaultCacheDuration = configuration.GetValue<int>("Cache:DefaultDurationSeconds", 300);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 只缓存GET请求
            if (context.Request.Method != "GET")
            {
                await _next(context);
                return;
            }

            var cacheKey = GenerateCacheKey(context);
            if (_cache.TryGetValue(cacheKey, out var cachedResponse))
            {
                LogHelper.Info("从缓存返回响应: {Path}", context.Request.Path);
                await WriteCachedResponse(context, cachedResponse);
                return;
            }

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status200OK)
            {
                var response = await GetResponse(context, responseBody);
                var cacheDuration = GetCacheDuration(context);
                
                _cache.Set(cacheKey, response, TimeSpan.FromSeconds(cacheDuration));
                LogHelper.Info($"缓存响应: {context.Request.Path} 有效期 {cacheDuration}秒");
            }

            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }

        private string GenerateCacheKey(HttpContext context)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append(context.Request.Path);
            keyBuilder.Append('?');
            keyBuilder.Append(context.Request.QueryString);
            return keyBuilder.ToString();
        }

        private async Task WriteCachedResponse(HttpContext context, object cachedResponse)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(cachedResponse);
        }

        private async Task<object> GetResponse(HttpContext context, MemoryStream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var response = await new StreamReader(responseBody).ReadToEndAsync();
            return System.Text.Json.JsonSerializer.Deserialize<object>(response);
        }

        private int GetCacheDuration(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Cache-Duration", out var durationHeader) &&
                int.TryParse(durationHeader, out var duration))
            {
                return duration;
            }
            return _defaultCacheDuration;
        }
    }
} 
