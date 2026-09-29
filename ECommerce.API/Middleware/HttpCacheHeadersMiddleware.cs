using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Middleware
{
    public class HttpCacheHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        // Targeted GET paths for HTTP cache headers/ETag
        private static readonly string[] TargetPrefixes = new[]
        {
            "/api/home/bestsellers",
            "/api/home/featured",
            "/api/Product/featured",
            "/api/Product/popular"
        };

        public HttpCacheHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method) || HasAuthHeader(context) || !MatchesTargetPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var originalBody = context.Response.Body;
            await using var mem = new MemoryStream();
            context.Response.Body = mem;

            try
            {
                await _next(context);

                // Only apply on successful responses
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    mem.Position = 0;
                    var bytes = mem.ToArray();
                    var etag = ComputeWeakETag(bytes);

                    // Add cache headers (public cache, CDN/edge friendly)
                    context.Response.Headers["Cache-Control"] = "public, max-age=60, s-maxage=600, stale-while-revalidate=30, stale-if-error=600";
                    context.Response.Headers["Vary"] = "Accept-Language"; // widen if currency/locale varies
                    context.Response.Headers["ETag"] = etag;

                    var ifNoneMatch = context.Request.Headers["If-None-Match"].ToString();
                    if (!string.IsNullOrEmpty(ifNoneMatch) && TagListContains(ifNoneMatch, etag))
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status304NotModified;
                        context.Response.Headers["ETag"] = etag;
                        return;
                    }

                    // Write captured body back to original response stream
                    mem.Position = 0;
                    await mem.CopyToAsync(originalBody);
                }
                else
                {
                    // Non-success: just pass-through
                    mem.Position = 0;
                    await mem.CopyToAsync(originalBody);
                }
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }

        private static bool HasAuthHeader(HttpContext ctx)
        {
            return ctx.Request.Headers.ContainsKey("Authorization") ||
                   ctx.User?.Identity?.IsAuthenticated == true;
        }

        private static bool MatchesTargetPath(PathString path)
        {
            var p = path.ToString();
            foreach (var prefix in TargetPrefixes)
            {
                if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static string ComputeWeakETag(ReadOnlySpan<byte> payload)
        {
            // SHA256 hash -> hex string, weak ETag format
            var hash = SHA256.HashData(payload);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return $"W/\"{sb}\"";
        }

        private static bool TagListContains(string etagList, string tag)
        {
            // If-None-Match can be a list: e.g. W/"abc", "def"
            var parts = etagList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var p in parts)
            {
                if (string.Equals(p, tag, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}

