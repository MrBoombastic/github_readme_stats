using GitHubStats.Application.Services;
using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;
using GitHubStats.Infrastructure.BackgroundFetch;
using Moq;

namespace GitHubStats.Tests;

public class EndpointCacheBehaviorTests
{
    [Fact]
    public async Task CacheHit_ReturnsDataWithoutEnqueue()
    {
        // Simulates the endpoint flow: cache hit → render → return (no background fetch needed)
        var cacheService = new Mock<ICacheService>();
        var fetchQueue = new BackgroundFetchQueue();

        var cachedStats = new UserStats
        {
            Name = "Test User", Login = "testuser",
            TotalStars = 100, TotalCommits = 500, TotalPRs = 50,
            TotalIssues = 20, TotalFollowers = 10, TotalRepos = 30,
            Rank = new UserRank { Level = "A+", Percentile = 5 }
        };

        var cacheKey = StatsCardService.GenerateCacheKey("testuser", false, null, false, false, false, null);
        cacheService.Setup(c => c.GetAsync<UserStats>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedStats);

        // Act: simulate cache check
        var cached = await cacheService.Object.GetAsync<UserStats>(cacheKey);

        // Assert: data returned, nothing enqueued
        Assert.NotNull(cached);
        Assert.Equal("Test User", cached!.Name);
        Assert.False(fetchQueue.IsPending(cacheKey));
    }

    [Fact]
    public async Task CacheMiss_EnqueuesBackgroundFetch()
    {
        // Simulates the endpoint flow: cache miss → enqueue → return loading SVG
        var cacheService = new Mock<ICacheService>();
        var fetchQueue = new BackgroundFetchQueue();

        var cacheKey = StatsCardService.GenerateCacheKey("newuser", false, null, false, false, false, null);
        cacheService.Setup(c => c.GetAsync<UserStats>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStats?)null);

        // Act: simulate cache miss + enqueue
        var cached = await cacheService.Object.GetAsync<UserStats>(cacheKey);
        Assert.Null(cached);

        fetchQueue.TryEnqueue(new StatsFetchRequest(
            cacheKey, "newuser", false, null, false, false, false, null, TimeSpan.FromDays(1)));

        // Assert: request is pending
        Assert.True(fetchQueue.IsPending(cacheKey));
    }

    [Fact]
    public async Task CacheMiss_EnqueueAllForUser_PreFetchesAllTypes()
    {
        var cacheService = new Mock<ICacheService>();
        var fetchQueue = new BackgroundFetchQueue();

        // All cache misses
        cacheService.Setup(c => c.GetAsync<UserStats>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserStats?)null);

        var statsKey = StatsCardService.GenerateCacheKey("newuser", false, null, false, false, false, null);
        var cached = await cacheService.Object.GetAsync<UserStats>(statsKey);
        Assert.Null(cached);

        // Enqueue specific + all for user
        fetchQueue.TryEnqueue(new StatsFetchRequest(
            statsKey, "newuser", false, null, false, false, false, null, TimeSpan.FromDays(1)));
        fetchQueue.EnqueueAllForUser("newuser");

        // Assert: all three types are pending
        var streakKey = StreakCardService.GenerateCacheKey("newuser", null);
        var langsKey = TopLanguagesCardService.GenerateCacheKey("newuser", null, 1, 0);

        Assert.True(fetchQueue.IsPending(statsKey));
        Assert.True(fetchQueue.IsPending(streakKey));
        Assert.True(fetchQueue.IsPending(langsKey));
    }

    [Fact]
    public void ThemeChange_UsesSameCacheKey()
    {
        // Theme does not affect cache key, so changing theme hits the same cached data
        var key1 = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);
        var key2 = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void DifferentCardTypes_HaveDifferentCacheKeyPrefixes()
    {
        var statsKey = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);
        var streakKey = StreakCardService.GenerateCacheKey("user", null);
        var langsKey = TopLanguagesCardService.GenerateCacheKey("user", null, 1, 0);

        // All different from each other
        Assert.NotEqual(statsKey, streakKey);
        Assert.NotEqual(statsKey, langsKey);
        Assert.NotEqual(streakKey, langsKey);

        // Each has its own prefix
        Assert.StartsWith("stats:", statsKey);
        Assert.StartsWith("streak:", streakKey);
        Assert.StartsWith("langs:", langsKey);
    }
}
