namespace GitHubStats.Infrastructure.BackgroundFetch;

/// <summary>
/// Represents a background data fetch request.
/// </summary>
public abstract record FetchRequest(string CacheKey);

public sealed record StatsFetchRequest(
    string CacheKey,
    string Username,
    bool IncludeAllCommits,
    IReadOnlyList<string>? ExcludeRepos,
    bool IncludeMergedPRs,
    bool IncludeDiscussions,
    bool IncludeDiscussionsAnswers,
    int? CommitsYear,
    TimeSpan CacheDuration) : FetchRequest(CacheKey);

public sealed record StreakFetchRequest(
    string CacheKey,
    string Username,
    int? StartingYear,
    TimeSpan CacheDuration) : FetchRequest(CacheKey);

public sealed record TopLangsFetchRequest(
    string CacheKey,
    string Username,
    IReadOnlyList<string>? ExcludeRepos,
    double SizeWeight,
    double CountWeight,
    TimeSpan CacheDuration) : FetchRequest(CacheKey);
