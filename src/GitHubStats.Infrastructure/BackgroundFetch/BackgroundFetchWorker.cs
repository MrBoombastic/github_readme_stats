using GitHubStats.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitHubStats.Infrastructure.BackgroundFetch;

/// <summary>
/// Background service that processes data fetch requests from the queue.
/// Populates cache so subsequent requests are served instantly.
/// Processes multiple requests concurrently for maximum throughput.
/// </summary>
public sealed class BackgroundFetchWorker : BackgroundService
{
    private readonly BackgroundFetchQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundFetchWorker> _logger;

    // Process up to 6 requests concurrently (2 users × 3 card types)
    private const int MaxConcurrency = 6;

    public BackgroundFetchWorker(
        BackgroundFetchQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundFetchWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundFetchWorker started (concurrency={MaxConcurrency})", MaxConcurrency);

        using var semaphore = new SemaphoreSlim(MaxConcurrency);

        await foreach (var request in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await semaphore.WaitAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessRequestAsync(request, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Background fetch failed for key: {CacheKey}", request.CacheKey);
                }
                finally
                {
                    _queue.MarkCompleted(request.CacheKey);
                    semaphore.Release();
                }
            }, stoppingToken);
        }
    }

    private async Task ProcessRequestAsync(FetchRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var gitHubClient = scope.ServiceProvider.GetRequiredService<IGitHubClient>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        switch (request)
        {
            case StatsFetchRequest stats:
                await FetchStatsAsync(gitHubClient, cacheService, stats, ct);
                break;
            case StreakFetchRequest streak:
                await FetchStreakAsync(gitHubClient, cacheService, streak, ct);
                break;
            case TopLangsFetchRequest langs:
                await FetchTopLangsAsync(gitHubClient, cacheService, langs, ct);
                break;
        }
    }

    private async Task FetchStatsAsync(IGitHubClient client, ICacheService cache, StatsFetchRequest req, CancellationToken ct)
    {
        _logger.LogDebug("Background fetching stats for {Username}", req.Username);
        var data = await client.GetUserStatsAsync(
            req.Username, req.IncludeAllCommits, req.ExcludeRepos,
            req.IncludeMergedPRs, req.IncludeDiscussions, req.IncludeDiscussionsAnswers,
            req.CommitsYear, ct);
        await cache.SetAsync(req.CacheKey, data, req.CacheDuration, ct);
        _logger.LogInformation("Background fetch completed: stats for {Username}", req.Username);
    }

    private async Task FetchStreakAsync(IGitHubClient client, ICacheService cache, StreakFetchRequest req, CancellationToken ct)
    {
        _logger.LogDebug("Background fetching streak for {Username}", req.Username);
        var data = await client.GetStreakStatsAsync(req.Username, req.StartingYear, ct);
        await cache.SetAsync(req.CacheKey, data, req.CacheDuration, ct);
        _logger.LogInformation("Background fetch completed: streak for {Username}", req.Username);
    }

    private async Task FetchTopLangsAsync(IGitHubClient client, ICacheService cache, TopLangsFetchRequest req, CancellationToken ct)
    {
        _logger.LogDebug("Background fetching top-langs for {Username}", req.Username);
        var data = await client.GetTopLanguagesAsync(
            req.Username, req.ExcludeRepos, req.SizeWeight, req.CountWeight, ct);
        await cache.SetAsync(req.CacheKey, data, req.CacheDuration, ct);
        _logger.LogInformation("Background fetch completed: top-langs for {Username}", req.Username);
    }
}
