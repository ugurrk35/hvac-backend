using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace ECommerce.API.Helper
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheResponseAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _ttlSeconds;

        public CacheResponseAttribute(int ttlSeconds = 60)
        {
            _ttlSeconds = ttlSeconds;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var services = http.RequestServices;
            var configuration = services.GetService<IConfiguration>();
            var logger = services.GetService<ILogger<CacheResponseAttribute>>();

            // ✅ Sadece GET istekleri cachelenir
            if (!HttpMethods.IsGet(http.Request.Method))
            {
                await next();
                return;
            }

            // ⚙️ Redis cache feature flag
            var enabledSetting = configuration?["RedisCacheEnabled"];
            if (bool.TryParse(enabledSetting, out var isEnabled) && !isEnabled)
            {
                await next();
                return;
            }

            // ⚙️ Cache bypass header'ları kontrol et
            var noCacheHeader = http.Request.Headers["Cache-Control"].ToString()
                .Contains("no-cache", StringComparison.OrdinalIgnoreCase);
            var bypassHeader = http.Request.Headers["X-Bypass-Cache"].ToString()
                .Equals("true", StringComparison.OrdinalIgnoreCase);

            //if (noCacheHeader || bypassHeader)
            //{
            //    http.Response.Headers["X-Cache-Hit"] = "bypass";
            //    logger?.LogInformation("Cache bypassed for {Path}", http.Request.Path);
            //    await next();
            //    return;
            //}

            // 🔑 Cache key oluştur
            var path = http.Request.Path.ToString();
            var query = http.Request.QueryString.HasValue ? http.Request.QueryString.Value : string.Empty;
            var key = RedisCacheKeys.ApiResponse(configuration, path, query);

            string? cachedJson = null;
            IDatabase? db = null;

            try
            {
                var redis = services.GetRequiredService<IConnectionMultiplexer>();
                db = redis.GetDatabase();

                var cached = await db.StringGetAsync(key);
                if (cached.HasValue)
                {
                    cachedJson = cached.ToString();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Redis GET failed for key {CacheKey}", key);
            }

            // ✅ Cache'de varsa direkt dön
            if (!string.IsNullOrEmpty(cachedJson))
            {
                http.Response.Headers["X-Cache-Hit"] = "true";

                context.Result = new ContentResult
                {
                    Content = cachedJson,
                    ContentType = "application/json",
                    StatusCode = 200
                };
                return;
            }

            // ⏩ Cache yoksa API normal çalışsın
            var executed = await next();

            try
            {
                if (db != null && executed.Result is ObjectResult objectResult && objectResult.Value != null)
                {
                    var json = objectResult.Value is string s
                        ? s
                        : JsonConvert.SerializeObject(
    objectResult.Value,
    new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    });

                    await db.StringSetAsync(key, json, TimeSpan.FromSeconds(_ttlSeconds));
                    http.Response.Headers["X-Cache-Hit"] = "false";
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Redis SET failed for key {CacheKey}", key);
            }
        }
    }
}
