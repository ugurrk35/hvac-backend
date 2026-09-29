using Microsoft.Extensions.Configuration;

namespace ECommerce.API.Helper;

public static class RedisCacheKeys
{
    public static string Prefix(IConfiguration? configuration)
    {
        var prefix = configuration?["Redis:KeyPrefix"] ?? "kombiklimaburada";
        return prefix.EndsWith(':') ? prefix : $"{prefix}:";
    }

    public static string ApiResponse(IConfiguration? configuration, string path, string query = "")
    {
        var normalizedPath = path.Trim('/').Replace('/', ':').ToLowerInvariant();
        return $"{Prefix(configuration)}{normalizedPath}_{query}".ToLowerInvariant();
    }

    public static string BlogCommentRateLimit(IConfiguration? configuration, string scope)
        => $"{Prefix(configuration)}rate-limit:blog-comment:{scope}";
}
