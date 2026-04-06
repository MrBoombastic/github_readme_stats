using GitHubStats.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitHubStats.Infrastructure.Caching;

/// <summary>
/// Background job that runs every 12 hours (9AM and 9PM SGT) to refresh
/// cached data for all tracked users. This ensures returning users always
/// get fresh data from cache without waiting for GitHub API calls.
/// </summary>
public sealed class CacheRefreshJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheRefreshJob> _logger;

    // 9AM SGT = 1AM UTC, runs every 12 hours
    private static readonly TimeZoneInfo SgtZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
    private static readonly TimeSpan TargetTimeSgt = new(9, 0, 0); // 9:00 AM SGT
    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);

    public CacheRefreshJob(IServiceScopeFactory scopeFactory, ILogger<CacheRefreshJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CacheRefreshJob started — runs at 9AM and 9PM SGT");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            _logger.LogInformation("Next cache refresh in {Hours:F1} hours", delay.TotalHours);

            await Task.Delay(delay, stoppingToken);

            try
            {
                await RefreshAllUsersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache refresh failed");
            }
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var nowUtc = DateTime.UtcNow;
        var nowSgt = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SgtZone);

        // Find next 9AM or 9PM SGT
        var today9am = nowSgt.Date + TargetTimeSgt;
        var today9pm = today9am + Interval;
        var tomorrow9am = today9am.AddDays(1);

        DateTime nextRunSgt;
        if (nowSgt < today9am)
            nextRunSgt = today9am;
        else if (nowSgt < today9pm)
            nextRunSgt = today9pm;
        else
            nextRunSgt = tomorrow9am;

        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunSgt, SgtZone);
        var delay = nextRunUtc - nowUtc;

        // Safety: minimum 1 minute delay to avoid tight loops
        return delay < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delay;
    }

    private async Task RefreshAllUsersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<UserTracker>();
        var gitHubClient = scope.ServiceProvider.GetRequiredService<IGitHubClient>();
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var users = await tracker.GetAllAsync(ct);
        if (users.Count == 0)
        {
            _logger.LogInformation("No tracked users to refresh");
            return;
        }

        _logger.LogInformation("Refreshing cache for {Count} tracked users", users.Count);
        var refreshed = 0;
        var failed = 0;

        foreach (var username in users)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await RefreshUserAsync(username, gitHubClient, cacheService, ct);
                refreshed++;
                _logger.LogDebug("Refreshed cache for {Username}", username);
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Failed to refresh cache for {Username}", username);
            }

            // Small delay between users to avoid hammering GitHub API
            await Task.Delay(500, ct);
        }

        _logger.LogInformation("Cache refresh complete: {Refreshed} refreshed, {Failed} failed out of {Total}",
            refreshed, failed, users.Count);
    }

    private static async Task RefreshUserAsync(
        string username, IGitHubClient client, ICacheService cache, CancellationToken ct)
    {
        // Fetch all 3 types in parallel
        var statsTask = Task.Run(async () =>
        {
            var data = await client.GetUserStatsAsync(username, cancellationToken: ct);
            var key = Application.Services.StatsCardService.GenerateCacheKey(
                username, false, null, false, false, false, null);
            await cache.SetAsync(key, data, TimeSpan.FromDays(1), ct);
        }, ct);

        var streakTask = Task.Run(async () =>
        {
            var data = await client.GetStreakStatsAsync(username, cancellationToken: ct);
            var key = Application.Services.StreakCardService.GenerateCacheKey(username, null);
            await cache.SetAsync(key, data, TimeSpan.FromHours(3), ct);
        }, ct);

        var langsTask = Task.Run(async () =>
        {
            var data = await client.GetTopLanguagesAsync(username, cancellationToken: ct);
            var key = Application.Services.TopLanguagesCardService.GenerateCacheKey(username, null, 1, 0, false);
            await cache.SetAsync(key, data, TimeSpan.FromDays(6), ct);
        }, ct);

        await Task.WhenAll(statsTask, streakTask, langsTask);
    }
}
