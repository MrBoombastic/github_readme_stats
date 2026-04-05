using System.Collections.Concurrent;
using System.Threading.Channels;
using GitHubStats.Application.Services;

namespace GitHubStats.Infrastructure.BackgroundFetch;

/// <summary>
/// Thread-safe queue for background data fetch requests with deduplication.
/// </summary>
public sealed class BackgroundFetchQueue
{
    private readonly Channel<FetchRequest> _channel = Channel.CreateBounded<FetchRequest>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    // Track in-flight requests to avoid duplicate fetches
    private readonly ConcurrentDictionary<string, byte> _pending = new();

    /// <summary>
    /// Enqueues a fetch request if not already pending.
    /// Returns true if enqueued, false if already in progress.
    /// </summary>
    public bool TryEnqueue(FetchRequest request)
    {
        if (!_pending.TryAdd(request.CacheKey, 0))
            return false;

        if (!_channel.Writer.TryWrite(request))
        {
            _pending.TryRemove(request.CacheKey, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Enqueues all three card types for a user so switching between
    /// stats/streak/top-langs is instant after any one is first requested.
    /// Deduplication ensures already-pending or already-cached types are skipped.
    /// </summary>
    public void EnqueueAllForUser(string username)
    {
        // Stats with default params
        var statsKey = StatsCardService.GenerateCacheKey(
            username, includeAllCommits: false, excludeRepos: null,
            includeMergedPRs: false, includeDiscussions: false,
            includeDiscussionsAnswers: false, commitsYear: null);
        TryEnqueue(new StatsFetchRequest(
            statsKey, username,
            IncludeAllCommits: false, ExcludeRepos: null,
            IncludeMergedPRs: false, IncludeDiscussions: false,
            IncludeDiscussionsAnswers: false, CommitsYear: null,
            CacheDuration: TimeSpan.FromDays(1)));

        // Streak with default params
        var streakKey = StreakCardService.GenerateCacheKey(username, startingYear: null);
        TryEnqueue(new StreakFetchRequest(
            streakKey, username,
            StartingYear: null,
            CacheDuration: TimeSpan.FromHours(3)));

        // Top-langs with default params
        var langsKey = TopLanguagesCardService.GenerateCacheKey(
            username, excludeRepos: null, sizeWeight: 1, countWeight: 0);
        TryEnqueue(new TopLangsFetchRequest(
            langsKey, username,
            ExcludeRepos: null, SizeWeight: 1, CountWeight: 0,
            CacheDuration: TimeSpan.FromDays(6)));
    }

    /// <summary>
    /// Marks a request as completed (removes from pending set).
    /// </summary>
    public void MarkCompleted(string cacheKey)
    {
        _pending.TryRemove(cacheKey, out _);
    }

    /// <summary>
    /// Returns true if a fetch for this cache key is already pending.
    /// </summary>
    public bool IsPending(string cacheKey) => _pending.ContainsKey(cacheKey);

    public ChannelReader<FetchRequest> Reader => _channel.Reader;
}
