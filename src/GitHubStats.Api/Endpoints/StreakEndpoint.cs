using GitHubStats.Application.Services;
using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using GitHubStats.Infrastructure.BackgroundFetch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class StreakEndpoint
{
    public static void MapStreakEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/streak", async (
            [FromQuery] string? username,
            [FromQuery(Name = "hide_border")] bool? hideBorder,
            [FromQuery] string? theme,
            [FromQuery(Name = "title_color")] string? titleColor,
            [FromQuery(Name = "text_color")] string? textColor,
            [FromQuery(Name = "icon_color")] string? iconColor,
            [FromQuery(Name = "bg_color")] string? bgColor,
            [FromQuery(Name = "border_color")] string? borderColor,
            [FromQuery(Name = "border_radius")] double? borderRadius,
            [FromQuery(Name = "ring_color")] string? ringColor,
            [FromQuery(Name = "fire_color")] string? fireColor,
            [FromQuery(Name = "stroke_color")] string? strokeColor,
            [FromQuery(Name = "curr_streak_num_color")] string? currStreakNumColor,
            [FromQuery(Name = "side_nums_color")] string? sideNumsColor,
            [FromQuery(Name = "curr_streak_label_color")] string? currStreakLabelColor,
            [FromQuery(Name = "side_labels_color")] string? sideLabelsColor,
            [FromQuery(Name = "dates_color")] string? datesColor,
            [FromQuery(Name = "date_format")] string? dateFormat,
            [FromQuery(Name = "card_width")] int? cardWidth,
            [FromQuery(Name = "card_height")] int? cardHeight,
            [FromQuery(Name = "hide_total_contributions")] bool? hideTotalContributions,
            [FromQuery(Name = "hide_current_streak")] bool? hideCurrentStreak,
            [FromQuery(Name = "hide_longest_streak")] bool? hideLongestStreak,
            [FromQuery(Name = "starting_year")] int? startingYear,
            [FromQuery(Name = "cache_seconds")] int? cacheSeconds,
            [FromQuery] string? locale,
            [FromQuery(Name = "disable_animations")] bool? disableAnimations,
            StreakCardService service,
            ICardRenderer renderer,
            ICacheService cacheService,
            BackgroundFetchQueue fetchQueue,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(
                    renderer.RenderErrorCard("Missing required parameter: username"),
                    "image/svg+xml");
            }

            var options = new StreakCardOptions
            {
                Theme = theme,
                TitleColor = titleColor,
                TextColor = textColor,
                IconColor = iconColor,
                BgColor = bgColor,
                BorderColor = borderColor,
                BorderRadius = borderRadius,
                HideBorder = hideBorder ?? false,
                Locale = locale,
                DisableAnimations = disableAnimations ?? false,
                RingColor = ringColor,
                FireColor = fireColor,
                StrokeColor = strokeColor,
                CurrStreakNumColor = currStreakNumColor,
                SideNumsColor = sideNumsColor,
                CurrStreakLabelColor = currStreakLabelColor,
                SideLabelsColor = sideLabelsColor,
                DatesColor = datesColor,
                DateFormat = dateFormat ?? "M j[, Y]",
                CardWidth = cardWidth,
                CardHeight = cardHeight,
                HideTotalContributions = hideTotalContributions ?? false,
                HideCurrentStreak = hideCurrentStreak ?? false,
                HideLongestStreak = hideLongestStreak ?? false,
                StartingYear = startingYear
            };

            var cacheDuration = cacheSeconds.HasValue ? TimeSpan.FromSeconds(cacheSeconds.Value) : TimeSpan.FromHours(3);
            var cacheKey = StreakCardService.GenerateCacheKey(username, startingYear);

            // Try cache first (instant)
            var cached = await cacheService.GetAsync<StreakStats>(cacheKey, cancellationToken);
            if (cached != null)
            {
                var svg = renderer.RenderStreakCard(cached, options);
                SetCacheHeaders(context, cacheSeconds ?? 1800);
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(svg, "image/svg+xml");
            }

            // Cache miss - enqueue this specific fetch + pre-fetch all card types for this user
            fetchQueue.TryEnqueue(new StreakFetchRequest(
                cacheKey, username, startingYear, cacheDuration));
            fetchQueue.EnqueueAllForUser(username);

            SetLoadingCacheHeaders(context);
            context.Response.ContentType = "image/svg+xml";
            return Results.Content(
                renderer.RenderLoadingCard("streak", theme, bgColor, textColor, borderColor,
                    hideBorder ?? false, borderRadius),
                "image/svg+xml");
        })
        .WithName("GetStreak")
        .WithTags("Streak")
        .RequireRateLimiting("perIp");
    }

    private static void SetCacheHeaders(HttpContext context, int seconds)
    {
        context.Response.Headers.CacheControl = $"max-age={seconds}, s-maxage={seconds}, stale-while-revalidate=86400";
    }

    private static void SetLoadingCacheHeaders(HttpContext context)
    {
        context.Response.Headers.CacheControl = "max-age=1, s-maxage=1, stale-while-revalidate=0";
    }
}
