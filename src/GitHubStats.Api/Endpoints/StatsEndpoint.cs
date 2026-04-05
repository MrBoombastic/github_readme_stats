using GitHubStats.Application.Services;
using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Exceptions;
using GitHubStats.Domain.Interfaces;
using GitHubStats.Infrastructure.BackgroundFetch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GitHubStats.Api.Endpoints;

public static class StatsEndpoint
{
    public static void MapStatsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stats", async (
            [FromQuery] string? username,
            [FromQuery] string? hide,
            [FromQuery] string? show,
            [FromQuery(Name = "hide_title")] bool? hideTitle,
            [FromQuery(Name = "hide_border")] bool? hideBorder,
            [FromQuery(Name = "hide_rank")] bool? hideRank,
            [FromQuery(Name = "show_icons")] bool? showIcons,
            [FromQuery(Name = "include_all_commits")] bool? includeAllCommits,
            [FromQuery(Name = "commits_year")] int? commitsYear,
            [FromQuery] string? theme,
            [FromQuery(Name = "title_color")] string? titleColor,
            [FromQuery(Name = "text_color")] string? textColor,
            [FromQuery(Name = "icon_color")] string? iconColor,
            [FromQuery(Name = "bg_color")] string? bgColor,
            [FromQuery(Name = "border_color")] string? borderColor,
            [FromQuery(Name = "border_radius")] double? borderRadius,
            [FromQuery(Name = "ring_color")] string? ringColor,
            [FromQuery(Name = "cache_seconds")] int? cacheSeconds,
            [FromQuery] string? locale,
            [FromQuery(Name = "disable_animations")] bool? disableAnimations,
            [FromQuery(Name = "rank_icon")] string? rankIcon,
            [FromQuery(Name = "number_format")] string? numberFormat,
            [FromQuery(Name = "text_bold")] bool? textBold,
            [FromQuery(Name = "exclude_repo")] string? excludeRepo,
            [FromQuery(Name = "line_height")] int? lineHeight,
            [FromQuery(Name = "card_width")] int? cardWidth,
            StatsCardService service,
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

            var options = new StatsCardOptions
            {
                Theme = theme,
                TitleColor = titleColor,
                TextColor = textColor,
                IconColor = iconColor,
                BgColor = bgColor,
                BorderColor = borderColor,
                BorderRadius = borderRadius,
                HideBorder = hideBorder ?? false,
                HideTitle = hideTitle ?? false,
                Locale = locale,
                DisableAnimations = disableAnimations ?? false,
                Hide = hide?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Show = show?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                ShowIcons = showIcons ?? false,
                HideRank = hideRank ?? false,
                IncludeAllCommits = includeAllCommits ?? false,
                CommitsYear = commitsYear,
                LineHeight = lineHeight,
                CardWidth = cardWidth,
                RingColor = ringColor,
                TextBold = textBold ?? true,
                NumberFormat = numberFormat ?? "short",
                RankIcon = rankIcon ?? "default"
            };

            var excludeRepos = excludeRepo?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            var incAllCommits = includeAllCommits ?? false;
            var incMergedPRs = show?.Contains("prs_merged") ?? false;
            var incDiscussions = show?.Contains("discussions_started") ?? false;
            var incDiscussionsAnswers = show?.Contains("discussions_answered") ?? false;
            var cacheDuration = cacheSeconds.HasValue ? TimeSpan.FromSeconds(cacheSeconds.Value) : TimeSpan.FromDays(1);

            var cacheKey = StatsCardService.GenerateCacheKey(
                username, incAllCommits, excludeRepos, incMergedPRs,
                incDiscussions, incDiscussionsAnswers, commitsYear);

            // Try cache first (instant)
            var cached = await cacheService.GetAsync<UserStats>(cacheKey, cancellationToken);
            if (cached != null)
            {
                // Cache hit - render and return immediately
                var svg = renderer.RenderStatsCard(cached, options);
                SetCacheHeaders(context, cacheSeconds ?? 1800);
                context.Response.ContentType = "image/svg+xml";
                return Results.Content(svg, "image/svg+xml");
            }

            // Cache miss - enqueue this specific fetch + pre-fetch all card types for this user
            fetchQueue.TryEnqueue(new StatsFetchRequest(
                cacheKey, username, incAllCommits, excludeRepos,
                incMergedPRs, incDiscussions, incDiscussionsAnswers,
                commitsYear, cacheDuration));
            fetchQueue.EnqueueAllForUser(username);

            SetLoadingCacheHeaders(context);
            context.Response.ContentType = "image/svg+xml";
            return Results.Content(
                renderer.RenderLoadingCard("stats", theme, bgColor, textColor, borderColor,
                    hideBorder ?? false, borderRadius),
                "image/svg+xml");
        })
        .WithName("GetStats")
        .WithTags("Stats")
        .RequireRateLimiting("stats");
    }

    private static void SetCacheHeaders(HttpContext context, int seconds)
    {
        context.Response.Headers.CacheControl = $"max-age={seconds}, s-maxage={seconds}, stale-while-revalidate=86400";
    }

    private static void SetLoadingCacheHeaders(HttpContext context)
    {
        // Short TTL so browsers/CDN proxies retry quickly after background fetch completes
        context.Response.Headers.CacheControl = "max-age=1, s-maxage=1, stale-while-revalidate=0";
    }
}
