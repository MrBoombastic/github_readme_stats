using System.Text.Json;
using GitHubStats.Application.Services;
using GitHubStats.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class ProgressEndpoint
{
    public static void MapProgressEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/progress", async (
            [FromQuery] string? username,
            IGitHubClient gitHubClient,
            ICacheService cacheService,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                context.Response.StatusCode = 400;
                return;
            }

            // Set up SSE
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            var writer = context.Response;

            async Task SendEvent(string eventType, string step, string status, string message)
            {
                var data = JsonSerializer.Serialize(new { step, status, message });
                await writer.WriteAsync($"event: {eventType}\ndata: {data}\n\n", cancellationToken);
                await writer.Body.FlushAsync(cancellationToken);
            }

            // Check which data is already cached
            var statsKey = StatsCardService.GenerateCacheKey(
                username, false, null, false, false, false, null);
            var streakKey = StreakCardService.GenerateCacheKey(username, null);
            var langsKey = TopLanguagesCardService.GenerateCacheKey(username, null, 1, 0);

            var statsCached = await cacheService.GetAsync<Domain.Entities.UserStats>(statsKey, cancellationToken);
            var streakCached = await cacheService.GetAsync<Domain.Entities.StreakStats>(streakKey, cancellationToken);
            var langsCached = await cacheService.GetAsync<Domain.Entities.TopLanguages>(langsKey, cancellationToken);

            var allCached = statsCached != null && streakCached != null && langsCached != null;

            if (allCached)
            {
                await SendEvent("step", "stats", "done", "Stats loaded from cache");
                await SendEvent("step", "streak", "done", "Streak loaded from cache");
                await SendEvent("step", "langs", "done", "Languages loaded from cache");
                await SendEvent("complete", "all", "done", "All data ready");
                return;
            }

            // Send initial connecting event
            await SendEvent("step", "init", "active", "Connecting to GitHub API...");

            // Launch all three fetches in parallel, streaming progress as each completes
            var tasks = new List<Task>();
            var statsDuration = TimeSpan.FromDays(1);
            var streakDuration = TimeSpan.FromHours(3);
            var langsDuration = TimeSpan.FromDays(6);

            if (statsCached == null)
            {
                await SendEvent("step", "stats", "active", "Fetching GitHub stats...");
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var data = await gitHubClient.GetUserStatsAsync(
                            username, false, null, false, false, false, null, cancellationToken);
                        await cacheService.SetAsync(statsKey, data, statsDuration, cancellationToken);
                        await SendEvent("step", "stats", "done", "Stats loaded");
                    }
                    catch (Exception ex)
                    {
                        await SendEvent("step", "stats", "error", $"Stats failed: {ex.Message}");
                    }
                }, cancellationToken));
            }
            else
            {
                await SendEvent("step", "stats", "done", "Stats loaded from cache");
            }

            if (streakCached == null)
            {
                await SendEvent("step", "streak", "active", "Fetching contribution streak...");
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var data = await gitHubClient.GetStreakStatsAsync(username, null, cancellationToken);
                        await cacheService.SetAsync(streakKey, data, streakDuration, cancellationToken);
                        await SendEvent("step", "streak", "done", "Streak loaded");
                    }
                    catch (Exception ex)
                    {
                        await SendEvent("step", "streak", "error", $"Streak failed: {ex.Message}");
                    }
                }, cancellationToken));
            }
            else
            {
                await SendEvent("step", "streak", "done", "Streak loaded from cache");
            }

            if (langsCached == null)
            {
                await SendEvent("step", "langs", "active", "Fetching top languages...");
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var data = await gitHubClient.GetTopLanguagesAsync(
                            username, null, 1, 0, cancellationToken);
                        await cacheService.SetAsync(langsKey, data, langsDuration, cancellationToken);
                        await SendEvent("step", "langs", "done", "Languages loaded");
                    }
                    catch (Exception ex)
                    {
                        await SendEvent("step", "langs", "error", $"Languages failed: {ex.Message}");
                    }
                }, cancellationToken));
            }
            else
            {
                await SendEvent("step", "langs", "done", "Languages loaded from cache");
            }

            // Wait for all parallel fetches to complete
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            await SendEvent("step", "init", "done", "Connected");
            await SendEvent("complete", "all", "done", "All data ready");
        })
        .WithName("Progress")
        .WithTags("Progress")
        .RequireRateLimiting("perIp");
    }
}
