using GitHubStats.Domain.Interfaces;
using GitHubStats.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GitHubStats.Tests;

public class UserTrackerTests
{
    private readonly Mock<ICacheService> _cache = new();
    private readonly UserTracker _tracker;

    public UserTrackerTests()
    {
        _tracker = new UserTracker(_cache.Object, NullLogger<UserTracker>.Instance);
    }

    [Fact]
    public async Task TrackAsync_NewUser_AddsToSet()
    {
        _cache.Setup(c => c.GetAsync<TrackedUsers>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackedUsers?)null);

        await _tracker.TrackAsync("alice");

        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.Is<TrackedUsers>(t => t.Usernames.Contains("alice")),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackAsync_ExistingUser_DoesNotDuplicate()
    {
        _cache.Setup(c => c.GetAsync<TrackedUsers>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackedUsers { Usernames = ["alice"] });

        await _tracker.TrackAsync("alice");

        // SetAsync should NOT be called since user already tracked
        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<TrackedUsers>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_Empty_ReturnsEmptySet()
    {
        _cache.Setup(c => c.GetAsync<TrackedUsers>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackedUsers?)null);

        var result = await _tracker.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_WithUsers_ReturnsAll()
    {
        _cache.Setup(c => c.GetAsync<TrackedUsers>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrackedUsers { Usernames = ["alice", "bob", "charlie"] });

        var result = await _tracker.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Contains("alice", result);
        Assert.Contains("bob", result);
        Assert.Contains("charlie", result);
    }

    [Fact]
    public async Task TrackAsync_MultipleUsers_AccumulatesAll()
    {
        TrackedUsers? stored = null;

        _cache.Setup(c => c.GetAsync<TrackedUsers>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);

        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<TrackedUsers>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<string, TrackedUsers, TimeSpan, CancellationToken>((_, t, _, _) => stored = t);

        await _tracker.TrackAsync("alice");
        await _tracker.TrackAsync("bob");

        Assert.NotNull(stored);
        Assert.Equal(2, stored!.Usernames.Count);
        Assert.Contains("alice", stored.Usernames);
        Assert.Contains("bob", stored.Usernames);
    }
}
