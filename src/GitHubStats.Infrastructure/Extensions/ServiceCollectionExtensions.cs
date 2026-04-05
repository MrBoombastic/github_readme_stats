using GitHubStats.Domain.Interfaces;
using GitHubStats.Infrastructure.BackgroundFetch;
using GitHubStats.Infrastructure.Caching;
using GitHubStats.Infrastructure.Configuration;
using GitHubStats.Infrastructure.GitHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace GitHubStats.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services with high-availability configuration.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<AccessControlOptions>(configuration.GetSection(AccessControlOptions.SectionName));

        // Token rotator (singleton for shared state)
        services.AddSingleton<TokenRotator>();

        // GitHub client with resilience
        services.AddHttpClient<IGitHubClient, GitHubClient>("GitHub", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "GitHubStats");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Retry policy with fast exponential backoff
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(200);
            options.Retry.BackoffType = DelayBackoffType.Exponential;

            // Circuit breaker
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

            // Total request timeout
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);

            // Per-attempt timeout
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
        });

        // Caching
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();

        if (!string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            // Use Redis for distributed caching (high scalability)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnectionString;
                options.InstanceName = cacheOptions.InstanceName;
            });
        }
        else
        {
            // Fallback to in-memory cache for development
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, RedisCacheService>();

        // Background fetch queue and worker for non-blocking first requests
        services.AddSingleton<BackgroundFetchQueue>();
        services.AddHostedService<BackgroundFetchWorker>();

        return services;
    }
}
