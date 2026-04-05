using GitHubStats.Infrastructure.BackgroundFetch;

namespace GitHubStats.Tests;

public class BackgroundFetchQueueTests
{
    [Fact]
    public void TryEnqueue_NewRequest_ReturnsTrue()
    {
        var queue = new BackgroundFetchQueue();
        var request = new StatsFetchRequest(
            "stats:testuser:False:0:False:False:False:", "testuser",
            false, null, false, false, false, null, TimeSpan.FromDays(1));

        var result = queue.TryEnqueue(request);

        Assert.True(result);
    }

    [Fact]
    public void TryEnqueue_DuplicateCacheKey_ReturnsFalse()
    {
        var queue = new BackgroundFetchQueue();
        var request1 = new StatsFetchRequest(
            "stats:testuser:False:0:False:False:False:", "testuser",
            false, null, false, false, false, null, TimeSpan.FromDays(1));
        var request2 = new StreakFetchRequest(
            "stats:testuser:False:0:False:False:False:", "testuser",
            null, TimeSpan.FromHours(3));

        queue.TryEnqueue(request1);
        var result = queue.TryEnqueue(request2);

        Assert.False(result);
    }

    [Fact]
    public void IsPending_AfterEnqueue_ReturnsTrue()
    {
        var queue = new BackgroundFetchQueue();
        var request = new StreakFetchRequest("streak:user:", "user", null, TimeSpan.FromHours(3));

        queue.TryEnqueue(request);

        Assert.True(queue.IsPending("streak:user:"));
    }

    [Fact]
    public void IsPending_BeforeEnqueue_ReturnsFalse()
    {
        var queue = new BackgroundFetchQueue();

        Assert.False(queue.IsPending("streak:user:"));
    }

    [Fact]
    public void MarkCompleted_RemovesFromPending()
    {
        var queue = new BackgroundFetchQueue();
        var request = new StreakFetchRequest("streak:user:", "user", null, TimeSpan.FromHours(3));

        queue.TryEnqueue(request);
        Assert.True(queue.IsPending("streak:user:"));

        queue.MarkCompleted("streak:user:");
        Assert.False(queue.IsPending("streak:user:"));
    }

    [Fact]
    public void MarkCompleted_AllowsReEnqueue()
    {
        var queue = new BackgroundFetchQueue();
        var request = new StreakFetchRequest("streak:user:", "user", null, TimeSpan.FromHours(3));

        queue.TryEnqueue(request);
        queue.MarkCompleted("streak:user:");

        var result = queue.TryEnqueue(request);
        Assert.True(result);
    }

    [Fact]
    public void EnqueueAllForUser_EnqueuesThreeRequests()
    {
        var queue = new BackgroundFetchQueue();

        queue.EnqueueAllForUser("testuser");

        // All three should be pending
        var statsKey = Application.Services.StatsCardService.GenerateCacheKey(
            "testuser", false, null, false, false, false, null);
        var streakKey = Application.Services.StreakCardService.GenerateCacheKey("testuser", null);
        var langsKey = Application.Services.TopLanguagesCardService.GenerateCacheKey("testuser", null, 1, 0);

        Assert.True(queue.IsPending(statsKey));
        Assert.True(queue.IsPending(streakKey));
        Assert.True(queue.IsPending(langsKey));
    }

    [Fact]
    public void EnqueueAllForUser_Deduplicates_WhenCalledTwice()
    {
        var queue = new BackgroundFetchQueue();

        queue.EnqueueAllForUser("testuser");
        // Second call should not fail — duplicates are silently skipped
        queue.EnqueueAllForUser("testuser");

        var statsKey = Application.Services.StatsCardService.GenerateCacheKey(
            "testuser", false, null, false, false, false, null);
        Assert.True(queue.IsPending(statsKey));
    }

    [Fact]
    public async Task Reader_ReceivesEnqueuedItems()
    {
        var queue = new BackgroundFetchQueue();
        var request = new TopLangsFetchRequest("langs:user:0:1:0", "user", null, 1, 0, TimeSpan.FromDays(6));

        queue.TryEnqueue(request);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var received = await queue.Reader.ReadAsync(cts.Token);

        Assert.Equal("langs:user:0:1:0", received.CacheKey);
        Assert.IsType<TopLangsFetchRequest>(received);
    }
}
