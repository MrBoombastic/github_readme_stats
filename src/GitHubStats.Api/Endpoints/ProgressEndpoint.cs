using System.Text.Json;
using GitHubStats.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class ProgressEndpoint
{
    public static void MapProgressEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/progress", async (
            [FromQuery] string? username,
            StatsCardService statsService,
            StreakCardService streakService,
            TopLanguagesCardService langsService,
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

            async Task SendEvent(string eventType, string step, string status, string message)
            {
                var data = JsonSerializer.Serialize(new { step, status, message });
                await context.Response.WriteAsync($"event: {eventType}\ndata: {data}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }

            await SendEvent("step", "init", "active", "Connecting to GitHub API...");

            // Launch all three fetches in parallel using card services (which handle caching internally)
            await SendEvent("step", "stats", "active", "Fetching GitHub stats...");
            await SendEvent("step", "streak", "active", "Fetching contribution streak...");
            await SendEvent("step", "langs", "active", "Fetching top languages...");

            var statsTask = Task.Run(async () =>
            {
                try
                {
                    await statsService.GetStatsAsync(username, cancellationToken: cancellationToken);
                    await SendEvent("step", "stats", "done", "Stats loaded");
                }
                catch (Exception ex)
                {
                    await SendEvent("step", "stats", "error", $"Stats failed: {ex.Message}");
                }
            }, cancellationToken);

            var streakTask = Task.Run(async () =>
            {
                try
                {
                    await streakService.GetStatsAsync(username, cancellationToken: cancellationToken);
                    await SendEvent("step", "streak", "done", "Streak loaded");
                }
                catch (Exception ex)
                {
                    await SendEvent("step", "streak", "error", $"Streak failed: {ex.Message}");
                }
            }, cancellationToken);

            var langsTask = Task.Run(async () =>
            {
                try
                {
                    await langsService.GetTopLanguagesAsync(username, cancellationToken: cancellationToken);
                    await SendEvent("step", "langs", "done", "Languages loaded");
                }
                catch (Exception ex)
                {
                    await SendEvent("step", "langs", "error", $"Languages failed: {ex.Message}");
                }
            }, cancellationToken);

            await Task.WhenAll(statsTask, streakTask, langsTask);

            await SendEvent("step", "init", "done", "Connected");
            await SendEvent("complete", "all", "done", "All data ready");
        })
        .WithName("Progress")
        .WithTags("Progress")
        .RequireRateLimiting("perIp");
    }
}
