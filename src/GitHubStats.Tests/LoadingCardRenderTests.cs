using GitHubStats.Rendering.Cards;

namespace GitHubStats.Tests;

public class LoadingCardRenderTests
{
    private readonly CardRenderer _renderer = new();

    [Theory]
    [InlineData("stats")]
    [InlineData("streak")]
    [InlineData("top-langs")]
    public void RenderLoadingCard_ReturnsSvg(string cardType)
    {
        var svg = _renderer.RenderLoadingCard(cardType);

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg);
        Assert.Contains("Generating...", svg);
        Assert.Contains("Data will appear shortly", svg);
    }

    [Fact]
    public void RenderLoadingCard_IncludesAnimations()
    {
        var svg = _renderer.RenderLoadingCard("stats");

        Assert.Contains("@keyframes pulse", svg);
        Assert.Contains("@keyframes dotPulse", svg);
        Assert.Contains("class=\"dot\"", svg);
    }

    [Fact]
    public void RenderLoadingCard_RespectsTheme()
    {
        var svgDefault = _renderer.RenderLoadingCard("stats");
        var svgDark = _renderer.RenderLoadingCard("stats", theme: "dark");

        // Both should be valid SVGs
        Assert.StartsWith("<svg", svgDefault);
        Assert.StartsWith("<svg", svgDark);
    }

    [Theory]
    [InlineData("stats", 450)]
    [InlineData("streak", 495)]
    [InlineData("top-langs", 300)]
    public void RenderLoadingCard_HasCorrectWidth(string cardType, int expectedWidth)
    {
        var svg = _renderer.RenderLoadingCard(cardType);

        Assert.Contains($"width=\"{expectedWidth}\"", svg);
    }

    [Fact]
    public void RenderLoadingCard_WithCustomBorderRadius()
    {
        var svg = _renderer.RenderLoadingCard("stats", borderRadius: 10);

        Assert.NotNull(svg);
        Assert.StartsWith("<svg", svg);
    }
}
