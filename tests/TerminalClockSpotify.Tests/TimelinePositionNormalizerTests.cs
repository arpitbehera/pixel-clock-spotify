using TerminalClockSpotify.Media;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class TimelinePositionNormalizerTests
{
    private static readonly DateTimeOffset UpdatedAt = DateTimeOffset.Parse("2026-05-31T12:00:00Z");

    [Fact]
    public void PlayingPositionIncludesTimelineAge() =>
        Assert.Equal(
            TimeSpan.FromSeconds(15),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(100),
                MediaPlaybackKind.Playing,
                UpdatedAt,
                UpdatedAt.AddSeconds(5)));

    [Theory]
    [InlineData(MediaPlaybackKind.Paused)]
    [InlineData(MediaPlaybackKind.Stopped)]
    [InlineData(MediaPlaybackKind.Unknown)]
    public void NonPlayingPositionIgnoresTimelineAge(MediaPlaybackKind playbackKind) =>
        Assert.Equal(
            TimeSpan.FromSeconds(10),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(100),
                playbackKind,
                UpdatedAt,
                UpdatedAt.AddSeconds(5)));

    [Fact]
    public void NegativeTimelineAgeDoesNotMovePositionBackward() =>
        Assert.Equal(
            TimeSpan.FromSeconds(10),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(100),
                MediaPlaybackKind.Playing,
                UpdatedAt,
                UpdatedAt.AddSeconds(-5)));

    [Fact]
    public void PositionClampsToDuration() =>
        Assert.Equal(
            TimeSpan.FromSeconds(100),
            TimelinePositionNormalizer.Normalize(
                TimeSpan.FromSeconds(98),
                TimeSpan.FromSeconds(100),
                MediaPlaybackKind.Playing,
                UpdatedAt,
                UpdatedAt.AddSeconds(5)));
}
