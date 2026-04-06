using GitHubStats.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GitHubStats.Infrastructure.Caching;

/// <summary>
/// Tracks usernames that have been requested so we can refresh their cache proactively.
/// Stored in distributed cache with a very long TTL.
/// </summary>
public sealed class UserTracker
{
    private readonly ICacheService _cache;
    private readonly ILogger<UserTracker> _logger;
    private const string CacheKey = "tracked-users";
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(365);

    public UserTracker(ICacheService cache, ILogger<UserTracker> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Adds a username to the tracked set. Idempotent.
    /// </summary>
    public async Task TrackAsync(string username, CancellationToken ct = default)
    {
        var users = await GetAllAsync(ct);
        if (users.Contains(username))
            return;

        users.Add(username);
        await _cache.SetAsync(CacheKey, new TrackedUsers { Usernames = users }, Ttl, ct);
        _logger.LogDebug("Tracking new user: {Username} (total: {Count})", username, users.Count);
    }

    /// <summary>
    /// Returns all tracked usernames.
    /// </summary>
    public async Task<HashSet<string>> GetAllAsync(CancellationToken ct = default)
    {
        var data = await _cache.GetAsync<TrackedUsers>(CacheKey, ct);
        return data?.Usernames ?? [];
    }
}

/// <summary>
/// Wrapper for serializing the username set to cache.
/// </summary>
public sealed class TrackedUsers
{
    public HashSet<string> Usernames { get; set; } = [];
}
