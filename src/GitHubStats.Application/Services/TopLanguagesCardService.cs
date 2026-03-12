using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;

namespace GitHubStats.Application.Services;

/// <summary>
/// Service for generating top languages cards with caching support.
/// </summary>
public sealed class TopLanguagesCardService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ICacheService _cacheService;
    private readonly ICardRenderer _cardRenderer;

    public TopLanguagesCardService(
        IGitHubClient gitHubClient,
        ICacheService cacheService,
        ICardRenderer cardRenderer)
    {
        _gitHubClient = gitHubClient;
        _cacheService = cacheService;
        _cardRenderer = cardRenderer;
    }

    /// <summary>
    /// Generates a top languages card SVG.
    /// </summary>
    public async Task<string> GenerateCardAsync(
        string username,
        TopLanguagesCardOptions options,
        IReadOnlyList<string>? excludeRepos = null,
        double sizeWeight = 1,
        double countWeight = 0,
        bool includeForks = false,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(username, excludeRepos, sizeWeight, countWeight, includeForks);

        var languages = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetTopLanguagesAsync(
                username, excludeRepos, sizeWeight, countWeight, includeForks, ct),
            cacheDuration ?? TimeSpan.FromDays(6),
            cancellationToken);

        return _cardRenderer.RenderTopLanguagesCard(languages, options);
    }

    /// <summary>
    /// Gets top languages data from cache or API.
    /// </summary>
    public async Task<TopLanguages> GetTopLanguagesAsync(
        string username,
        IReadOnlyList<string>? excludeRepos = null,
        double sizeWeight = 1,
        double countWeight = 0,
        bool includeForks = false,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(username, excludeRepos, sizeWeight, countWeight, includeForks);

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct => await _gitHubClient.GetTopLanguagesAsync(
                username, excludeRepos, sizeWeight, countWeight, includeForks, ct),
            cacheDuration ?? TimeSpan.FromDays(6),
            cancellationToken);
    }

    private static string GenerateCacheKey(
        string username,
        IReadOnlyList<string>? excludeRepos,
        double sizeWeight,
        double countWeight,
        bool includeForks)
    {
        var excludeHash = excludeRepos?.Count > 0
            ? string.Join(",", excludeRepos.OrderBy(r => r)).GetHashCode()
            : 0;

        return $"langs:{username}:{excludeHash}:{sizeWeight}:{countWeight}:{includeForks}";
    }
}
