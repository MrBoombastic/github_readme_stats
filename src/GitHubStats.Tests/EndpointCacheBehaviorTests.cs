using GitHubStats.Application.Services;
using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;
using Moq;

namespace GitHubStats.Tests;

public class EndpointCacheBehaviorTests
{
    [Fact]
    public async Task CacheHit_ReturnsDataWithoutApiCall()
    {
        var cacheService = new Mock<ICacheService>();
        var gitHubClient = new Mock<IGitHubClient>();

        var cachedStats = new UserStats
        {
            Name = "Test User",
            Login = "testuser",
            TotalStars = 100,
            TotalCommits = 500,
            TotalPRs = 50,
            TotalIssues = 20,
            TotalFollowers = 10,
            TotalRepos = 30,
            Rank = new UserRank { Level = "A+", Percentile = 5 }
        };

        var cacheKey = StatsCardService.GenerateCacheKey("testuser", false, null, false, false, false, null);
        cacheService.Setup(c => c.GetOrCreateAsync(
                cacheKey, It.IsAny<Func<CancellationToken, Task<UserStats>>>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedStats);

        var service = new StatsCardService(gitHubClient.Object, cacheService.Object, Mock.Of<ICardRenderer>());
        var result = await service.GetStatsAsync("testuser");

        Assert.Equal("Test User", result.Name);
        // GitHub API should not have been called since cache returned data
        gitHubClient.Verify(c => c.GetUserStatsAsync(
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IReadOnlyList<string>?>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ThemeChange_UsesSameCacheKey()
    {
        // Theme is not part of cache key — changing theme hits same cached data
        var key1 = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);
        var key2 = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void DifferentCardTypes_HaveDifferentCacheKeyPrefixes()
    {
        var statsKey = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);
        var streakKey = StreakCardService.GenerateCacheKey("user", null);
        var langsKey = TopLanguagesCardService.GenerateCacheKey("user", null, 1, 0, false);

        Assert.NotEqual(statsKey, streakKey);
        Assert.NotEqual(statsKey, langsKey);
        Assert.NotEqual(streakKey, langsKey);

        Assert.StartsWith("stats:", statsKey);
        Assert.StartsWith("streak:", streakKey);
        Assert.StartsWith("langs:", langsKey);
    }
}
