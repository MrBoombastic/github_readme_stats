namespace GitHubStats.Infrastructure.Configuration;

/// <summary>
/// Configuration options for caching.
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Redis connection string. If null, uses in-memory cache.
    /// Supports both StackExchange.Redis format (host:port,password=xxx,ssl=true)
    /// and URI format (rediss://user:password@host:port).
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Returns the connection string in StackExchange.Redis format,
    /// converting from URI format (rediss://) if necessary.
    /// </summary>
    public string? GetStackExchangeConnectionString()
    {
        if (string.IsNullOrEmpty(RedisConnectionString))
            return null;

        // Already in StackExchange.Redis format
        if (!RedisConnectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) &&
            !RedisConnectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            return RedisConnectionString;

        // Parse URI format: redis[s]://[user:password@]host:port
        var useSsl = RedisConnectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
        var uri = new Uri(RedisConnectionString);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 6379;
        var password = uri.UserInfo.Contains(':')
            ? Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[1])
            : null;

        var parts = new List<string> { $"{host}:{port}" };
        if (!string.IsNullOrEmpty(password))
            parts.Add($"password={password}");
        if (useSsl)
            parts.Add("ssl=true");
        parts.Add("abortConnect=false");

        return string.Join(",", parts);
    }

    /// <summary>
    /// Instance name prefix for Redis keys.
    /// </summary>
    public string InstanceName { get; set; } = "GitHubStats:";

    /// <summary>
    /// Default cache duration for stats cards in seconds.
    /// </summary>
    public int StatsCardTtlSeconds { get; set; } = 86400; // 1 day

    /// <summary>
    /// Default cache duration for top languages cards in seconds.
    /// </summary>
    public int TopLangsCardTtlSeconds { get; set; } = 518400; // 6 days

    /// <summary>
    /// Default cache duration for pin cards in seconds.
    /// </summary>
    public int PinCardTtlSeconds { get; set; } = 864000; // 10 days

    /// <summary>
    /// Default cache duration for gist cards in seconds.
    /// </summary>
    public int GistCardTtlSeconds { get; set; } = 172800; // 2 days

    /// <summary>
    /// Cache duration for error responses in seconds.
    /// </summary>
    public int ErrorTtlSeconds { get; set; } = 600; // 10 minutes
}
