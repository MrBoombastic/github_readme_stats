using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;
using GitHubStats.Rendering.Cards;

namespace GitHubStats.Tests;

public class CardRendererTests
{
    private readonly CardRenderer _renderer = new();

    [Fact]
    public void RenderStatsCard_ReturnsValidSvg()
    {
        var stats = new UserStats
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

        var svg = _renderer.RenderStatsCard(stats, new StatsCardOptions());

        Assert.StartsWith("<svg", svg);
        Assert.Contains("Test User", svg);
    }

    [Fact]
    public void RenderStatsCard_DifferentThemes_BothValidSvg()
    {
        var stats = new UserStats
        {
            Name = "User",
            Login = "user",
            TotalStars = 10,
            TotalCommits = 50,
            TotalPRs = 5,
            TotalIssues = 2,
            TotalFollowers = 1,
            TotalRepos = 3,
            Rank = new UserRank { Level = "B+", Percentile = 30 }
        };

        var svgDefault = _renderer.RenderStatsCard(stats, new StatsCardOptions { Theme = "default" });
        var svgDark = _renderer.RenderStatsCard(stats, new StatsCardOptions { Theme = "dark" });

        Assert.StartsWith("<svg", svgDefault);
        Assert.StartsWith("<svg", svgDark);
        Assert.NotEqual(svgDefault, svgDark);
    }

    [Fact]
    public void RenderStreakCard_ReturnsValidSvg()
    {
        var stats = new StreakStats
        {
            Username = "testuser",
            TotalContributions = 1000,
            CurrentStreak = new StreakInfo { Length = 5, Start = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)), End = DateOnly.FromDateTime(DateTime.Now) },
            LongestStreak = new StreakInfo { Length = 30, Start = new DateOnly(2024, 1, 1), End = new DateOnly(2024, 1, 30) },
            FirstContribution = new DateOnly(2020, 1, 1)
        };

        var svg = _renderer.RenderStreakCard(stats, new StreakCardOptions());

        Assert.StartsWith("<svg", svg);
        Assert.Contains("1000", svg); // total contributions
    }

    [Fact]
    public void RenderTopLanguagesCard_ReturnsValidSvg()
    {
        var langs = new TopLanguages
        {
            Languages = new List<LanguageStats>
            {
                new() { Name = "C#", Color = "#178600", Size = 50000, RepoCount = 5, Percentage = 60 },
                new() { Name = "JavaScript", Color = "#f1e05a", Size = 30000, RepoCount = 3, Percentage = 40 }
            },
            TotalSize = 80000
        };

        var svg = _renderer.RenderTopLanguagesCard(langs, new TopLanguagesCardOptions());

        Assert.StartsWith("<svg", svg);
        Assert.Contains("C#", svg);
    }

    [Fact]
    public void RenderErrorCard_ReturnsValidSvg()
    {
        var svg = _renderer.RenderErrorCard("Something went wrong");

        Assert.StartsWith("<svg", svg);
        Assert.Contains("Something went wrong", svg);
    }

    [Fact]
    public void RenderErrorCard_WithSecondaryMessage()
    {
        var svg = _renderer.RenderErrorCard("Primary", "Secondary");

        Assert.StartsWith("<svg", svg);
        Assert.Contains("Primary", svg);
        Assert.Contains("Secondary", svg);
    }
}
