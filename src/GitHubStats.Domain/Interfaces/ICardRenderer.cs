using GitHubStats.Domain.Entities;

namespace GitHubStats.Domain.Interfaces;

/// <summary>
/// Interface for SVG card rendering operations.
/// </summary>
public interface ICardRenderer
{
    /// <summary>
    /// Renders a stats card SVG.
    /// </summary>
    string RenderStatsCard(UserStats stats, StatsCardOptions options);

    /// <summary>
    /// Renders a repository card SVG.
    /// </summary>
    string RenderRepoCard(Repository repo, RepoCardOptions options);

    /// <summary>
    /// Renders a top languages card SVG.
    /// </summary>
    string RenderTopLanguagesCard(TopLanguages languages, TopLanguagesCardOptions options);

    /// <summary>
    /// Renders a gist card SVG.
    /// </summary>
    string RenderGistCard(Gist gist, GistCardOptions options);

    /// <summary>
    /// Renders a streak card SVG.
    /// </summary>
    string RenderStreakCard(StreakStats stats, StreakCardOptions options);

    /// <summary>
    /// Renders an error card SVG.
    /// </summary>
    string RenderErrorCard(string message, string? secondaryMessage = null, CardColors? colors = null);
}

/// <summary>
/// Base card options with common styling properties.
/// </summary>
public record CardOptions
{
    /// <summary>
    /// Gets or sets the card theme (e.g., "default", "dark", "tokyonight").
    /// </summary>
    public string? Theme { get; init; }

    /// <summary>
    /// Gets or sets a custom title color (HEX).
    /// </summary>
    public string? TitleColor { get; init; }

    /// <summary>
    /// Gets or sets a custom text color (HEX).
    /// </summary>
    public string? TextColor { get; init; }

    /// <summary>
    /// Gets or sets a custom icon color (HEX).
    /// </summary>
    public string? IconColor { get; init; }

    /// <summary>
    /// Gets or sets a custom background color (HEX).
    /// </summary>
    public string? BgColor { get; init; }

    /// <summary>
    /// Gets or sets a custom border color (HEX).
    /// </summary>
    public string? BorderColor { get; init; }

    /// <summary>
    /// Gets or sets the card corner radius. Default is 4.5.
    /// </summary>
    public double? BorderRadius { get; init; }

    /// <summary>
    /// Gets or sets whether to hide the card border.
    /// </summary>
    public bool HideBorder { get; init; }

    /// <summary>
    /// Gets or sets whether to hide the card title.
    /// </summary>
    public bool HideTitle { get; init; }

    /// <summary>
    /// Gets or sets a custom card title.
    /// </summary>
    public string? CustomTitle { get; init; }

    /// <summary>
    /// Gets or sets the locale for text (e.g., "en", "es").
    /// </summary>
    public string? Locale { get; init; }

    /// <summary>
    /// Gets or sets whether to disable animations.
    /// </summary>
    public bool DisableAnimations { get; init; }
}

/// <summary>
/// Card color configuration.
/// </summary>
public record CardColors
{
    /// <summary>
    /// Gets or sets the title color (HEX).
    /// </summary>
    public string TitleColor { get; init; } = "2f80ed";

    /// <summary>
    /// Gets or sets the text color (HEX).
    /// </summary>
    public string TextColor { get; init; } = "434d58";

    /// <summary>
    /// Gets or sets the icon color (HEX).
    /// </summary>
    public string IconColor { get; init; } = "4c71f2";

    /// <summary>
    /// Gets or sets the background color (HEX).
    /// </summary>
    public string BgColor { get; init; } = "fffefe";

    /// <summary>
    /// Gets or sets the border color (HEX).
    /// </summary>
    public string BorderColor { get; init; } = "e4e2e2";

    /// <summary>
    /// Gets or sets the rank ring color (HEX).
    /// </summary>
    public string? RingColor { get; init; }
}

/// <summary>
/// Stats card specific options.
/// </summary>
public record StatsCardOptions : CardOptions
{
    /// <summary>
    /// Gets or sets a list of stats to hide (e.g., "stars", "commits", "prs", "issues", "contribs").
    /// </summary>
    public IReadOnlyList<string>? Hide { get; init; }

    /// <summary>
    /// Gets or sets a list of extra stats to show (e.g., "reviews", "prs_merged", "discussions_started", "discussions_answered").
    /// </summary>
    public IReadOnlyList<string>? Show { get; init; }

    /// <summary>
    /// Gets or sets whether to show icons next to stat labels.
    /// </summary>
    public bool ShowIcons { get; init; }

    /// <summary>
    /// Gets or sets whether to hide the ranking circle.
    /// </summary>
    public bool HideRank { get; init; }

    /// <summary>
    /// Gets or sets whether to count total commits instead of just the last year.
    /// Note: This is a data-fetching option that affects the rank calculation.
    /// </summary>
    public bool IncludeAllCommits { get; init; }

    /// <summary>
    /// Gets or sets the specific year to count commits for.
    /// </summary>
    public int? CommitsYear { get; init; }

    /// <summary>
    /// Gets or sets the space between rows.
    /// </summary>
    public int? LineHeight { get; init; }

    /// <summary>
    /// Gets or sets a manual card width override.
    /// </summary>
    public int? CardWidth { get; init; }

    /// <summary>
    /// Gets or sets the color of the rank ring.
    /// </summary>
    public string? RingColor { get; init; }

    /// <summary>
    /// Gets or sets whether to use bold text for labels.
    /// </summary>
    public bool TextBold { get; init; } = true;

    /// <summary>
    /// Gets or sets the number formatting style ("short" or "long").
    /// </summary>
    public string NumberFormat { get; init; } = "short";

    /// <summary>
    /// Gets or sets the number of decimal places for formatted numbers.
    /// </summary>
    public int? NumberPrecision { get; init; }
}

/// <summary>
/// Repository card specific options.
/// </summary>
public record RepoCardOptions : CardOptions
{
    /// <summary>
    /// Gets or sets whether to show the repository owner's username.
    /// </summary>
    public bool ShowOwner { get; init; }

    /// <summary>
    /// Gets or sets the number of lines for the repository description.
    /// </summary>
    public int? DescriptionLinesCount { get; init; }
}

/// <summary>
/// Top languages card specific options.
/// </summary>
public record TopLanguagesCardOptions : CardOptions
{
    /// <summary>
    /// Gets or sets a list of languages to hide.
    /// </summary>
    public IReadOnlyList<string>? Hide { get; init; }

    /// <summary>
    /// Gets or sets the card layout ("normal", "compact", "donut", "donut-vertical", "pie").
    /// </summary>
    public string Layout { get; init; } = "normal";

    /// <summary>
    /// Gets or sets the number of languages to show (max 20).
    /// </summary>
    public int? LangsCount { get; init; }

    /// <summary>
    /// Gets or sets a manual card width override.
    /// </summary>
    public int? CardWidth { get; init; }

    /// <summary>
    /// Gets or sets a manual card height override.
    /// </summary>
    public int? CardHeight { get; init; }

    /// <summary>
    /// Gets or sets whether to hide progress bars in compact layout.
    /// </summary>
    public bool HideProgress { get; init; }

    /// <summary>
    /// Gets or sets the format for statistics display ("percentages" or "bytes").
    /// Default is "percentages".
    /// </summary>
    public string StatsFormat { get; init; } = "percentages";
}

/// <summary>
/// Gist card specific options.
/// </summary>
public record GistCardOptions : CardOptions
{
    /// <summary>
    /// Gets or sets whether to show the gist owner's username.
    /// </summary>
    public bool ShowOwner { get; init; }
}

/// <summary>
/// Streak card specific options.
/// </summary>
public record StreakCardOptions : CardOptions
{
    /// <summary>
    /// Gets or sets a manual card width override.
    /// </summary>
    public int? CardWidth { get; init; }

    /// <summary>
    /// Gets or sets a manual card height override.
    /// </summary>
    public int? CardHeight { get; init; }

    /// <summary>
    /// Gets or sets the color of the streak ring (HEX).
    /// </summary>
    public string? RingColor { get; init; }

    /// <summary>
    /// Gets or sets the color of the fire icon (HEX).
    /// </summary>
    public string? FireColor { get; init; }

    /// <summary>
    /// Gets or sets the color of the current streak number (HEX).
    /// </summary>
    public string? CurrStreakNumColor { get; init; }

    /// <summary>
    /// Gets or sets the color of the side numbers (HEX).
    /// </summary>
    public string? SideNumsColor { get; init; }

    /// <summary>
    /// Gets or sets the color of the current streak label (HEX).
    /// </summary>
    public string? CurrStreakLabelColor { get; init; }

    /// <summary>
    /// Gets or sets the color of the side labels (HEX).
    /// </summary>
    public string? SideLabelsColor { get; init; }

    /// <summary>
    /// Gets or sets the color of the date range text (HEX).
    /// </summary>
    public string? DatesColor { get; init; }

    /// <summary>
    /// Gets or sets the color of the SVG stroke (HEX).
    /// </summary>
    public string? StrokeColor { get; init; }

    /// <summary>
    /// Gets or sets the date format (e.g., "M j[, Y]").
    /// </summary>
    public string DateFormat { get; init; } = "M j[, Y]";

    /// <summary>
    /// Gets or sets whether to hide the total contributions section.
    /// </summary>
    public bool HideTotalContributions { get; init; }

    /// <summary>
    /// Gets or sets whether to hide the current streak section.
    /// </summary>
    public bool HideCurrentStreak { get; init; }

    /// <summary>
    /// Gets or sets whether to hide the longest streak section.
    /// </summary>
    public bool HideLongestStreak { get; init; }

    /// <summary>
    /// Gets or sets the start year for streak calculation.
    /// </summary>
    public int? StartingYear { get; init; }
}
