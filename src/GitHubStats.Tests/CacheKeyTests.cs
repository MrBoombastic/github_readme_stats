using GitHubStats.Application.Services;

namespace GitHubStats.Tests;

public class CacheKeyTests
{
    [Fact]
    public void StatsKey_DoesNotIncludeTheme()
    {
        var key1 = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);
        var key2 = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);

        // Same params = same key regardless of theme (theme is not a param)
        Assert.Equal(key1, key2);
        Assert.DoesNotContain("theme", key1);
    }

    [Fact]
    public void StatsKey_VariesByUsername()
    {
        var key1 = StatsCardService.GenerateCacheKey("alice", false, null, false, false, false, null);
        var key2 = StatsCardService.GenerateCacheKey("bob", false, null, false, false, false, null);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void StatsKey_VariesByIncludeAllCommits()
    {
        var key1 = StatsCardService.GenerateCacheKey("user", false, null, false, false, false, null);
        var key2 = StatsCardService.GenerateCacheKey("user", true, null, false, false, false, null);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void StreakKey_DoesNotIncludeTheme()
    {
        var key = StreakCardService.GenerateCacheKey("user", null);

        Assert.DoesNotContain("theme", key);
        Assert.StartsWith("streak:", key);
    }

    [Fact]
    public void StreakKey_VariesByStartingYear()
    {
        var key1 = StreakCardService.GenerateCacheKey("user", null);
        var key2 = StreakCardService.GenerateCacheKey("user", 2020);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void TopLangsKey_DoesNotIncludeTheme()
    {
        var key = TopLanguagesCardService.GenerateCacheKey("user", null, 1, 0);

        Assert.DoesNotContain("theme", key);
        Assert.StartsWith("langs:", key);
    }

    [Fact]
    public void TopLangsKey_VariesByWeights()
    {
        var key1 = TopLanguagesCardService.GenerateCacheKey("user", null, 1, 0);
        var key2 = TopLanguagesCardService.GenerateCacheKey("user", null, 0.5, 0.5);

        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void TopLangsKey_VariesByExcludeRepos()
    {
        var key1 = TopLanguagesCardService.GenerateCacheKey("user", null, 1, 0);
        var key2 = TopLanguagesCardService.GenerateCacheKey("user", new List<string> { "repo1" }, 1, 0);

        Assert.NotEqual(key1, key2);
    }
}
