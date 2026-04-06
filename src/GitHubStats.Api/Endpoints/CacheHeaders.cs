namespace GitHubStats.Api.Endpoints;

/// <summary>
/// Shared cache header helpers for all SVG card endpoints.
/// </summary>
internal static class CacheHeaders
{
    internal static void Set(HttpContext context, int seconds)
    {
        context.Response.Headers.CacheControl = $"max-age={seconds}, s-maxage={seconds}, stale-while-revalidate=86400";
    }

    internal static void SetError(HttpContext context)
    {
        context.Response.Headers.CacheControl = "max-age=600, s-maxage=600, stale-while-revalidate=86400";
    }
}
