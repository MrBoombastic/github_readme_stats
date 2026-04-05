using GitHubStats.Domain.Entities;
using GitHubStats.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GitHubStats.Tests;

public class OutputCacheIntegrationTests : IClassFixture<OutputCacheIntegrationTests.TestApp>
{
    private readonly HttpClient _client;
    private readonly Mock<IGitHubClient> _gitHubClient;

    public OutputCacheIntegrationTests(TestApp factory)
    {
        _gitHubClient = factory.MockGitHubClient;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StreakEndpoint_SecondRequest_ServesFromOutputCache()
    {
        var callCount = 0;
        _gitHubClient.Setup(c => c.GetStreakStatsAsync(
                "cacheuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return CreateStreakStats();
            });

        var response1 = await _client.GetAsync("/api/streak?username=cacheuser&theme=tokyonight");
        var response2 = await _client.GetAsync("/api/streak?username=cacheuser&theme=tokyonight");

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal("image/svg+xml", response1.Content.Headers.ContentType?.MediaType);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task StatsEndpoint_SecondRequest_ServesFromOutputCache()
    {
        var callCount = 0;
        _gitHubClient.Setup(c => c.GetUserStatsAsync(
                "statscacheuser", false, null, false, false, false, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return CreateUserStats();
            });

        var response1 = await _client.GetAsync("/api/stats?username=statscacheuser");
        var response2 = await _client.GetAsync("/api/stats?username=statscacheuser");

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task TopLangsEndpoint_SecondRequest_ServesFromOutputCache()
    {
        var callCount = 0;
        _gitHubClient.Setup(c => c.GetTopLanguagesAsync(
                "langscacheuser", null, 1, 0,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return CreateTopLanguages();
            });

        var response1 = await _client.GetAsync("/api/top-langs?username=langscacheuser");
        var response2 = await _client.GetAsync("/api/top-langs?username=langscacheuser");

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task StreakEndpoint_DifferentThemes_CachedSeparately()
    {
        var callCount = 0;
        _gitHubClient.Setup(c => c.GetStreakStatsAsync(
                "themeuser", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return CreateStreakStats();
            });

        var response1 = await _client.GetAsync("/api/streak?username=themeuser&theme=dracula");
        var response2 = await _client.GetAsync("/api/streak?username=themeuser&theme=nord");

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);

        var svg1 = await response1.Content.ReadAsStringAsync();
        var svg2 = await response2.Content.ReadAsStringAsync();
        Assert.NotEqual(svg1, svg2);
    }

    [Fact]
    public async Task StreakEndpoint_DifferentUsernames_CachedSeparately()
    {
        var callCount = 0;
        _gitHubClient.Setup(c => c.GetStreakStatsAsync(
                It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return CreateStreakStats();
            });

        await _client.GetAsync("/api/streak?username=userA");
        await _client.GetAsync("/api/streak?username=userB");

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task StatsEndpoint_SameQueryParams_AnyClient_ServesCache()
    {
        var callCount = 0;
        _gitHubClient.Setup(c => c.GetUserStatsAsync(
                "shareduser", false, null, false, false, false, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                return CreateUserStats();
            });

        // Simulate different clients by using different request headers
        var request1 = new HttpRequestMessage(HttpMethod.Get, "/api/stats?username=shareduser");
        request1.Headers.Add("X-Forwarded-For", "1.1.1.1");
        var response1 = await _client.SendAsync(request1);

        var request2 = new HttpRequestMessage(HttpMethod.Get, "/api/stats?username=shareduser");
        request2.Headers.Add("X-Forwarded-For", "2.2.2.2");
        var response2 = await _client.SendAsync(request2);

        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal(1, callCount);
    }

    private static StreakStats CreateStreakStats() => new()
    {
        Username = "testuser",
        TotalContributions = 100,
        CurrentStreak = new StreakInfo { Length = 5, Start = new DateOnly(2026, 1, 1), End = new DateOnly(2026, 1, 5) },
        LongestStreak = new StreakInfo { Length = 10, Start = new DateOnly(2025, 6, 1), End = new DateOnly(2025, 6, 10) },
        FirstContribution = new DateOnly(2020, 1, 1)
    };

    private static UserStats CreateUserStats() => new()
    {
        Name = "Test User",
        Login = "testuser",
        TotalStars = 50,
        TotalCommits = 200,
        TotalPRs = 30,
        TotalIssues = 10,
        TotalFollowers = 5,
        TotalRepos = 20,
        Rank = new UserRank { Level = "A+", Percentile = 10 }
    };

    private static TopLanguages CreateTopLanguages() => new()
    {
        Languages = new List<LanguageStats>
        {
            new() { Name = "C#", Color = "#178600", Size = 50000, RepoCount = 10, Percentage = 60 },
            new() { Name = "TypeScript", Color = "#2b7489", Size = 30000, RepoCount = 5, Percentage = 40 }
        },
        TotalSize = 80000
    };

    public class TestApp : WebApplicationFactory<Program>
    {
        public Mock<IGitHubClient> MockGitHubClient { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace IGitHubClient with mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IGitHubClient));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddSingleton(MockGitHubClient.Object);
            });
        }
    }
}
