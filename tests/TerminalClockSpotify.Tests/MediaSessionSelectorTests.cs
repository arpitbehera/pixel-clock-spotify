using TerminalClockSpotify.Media;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class MediaSessionSelectorTests
{
    [Fact]
    public void SelectPrefersPlayingSpotifySession()
    {
        var sessions = new[]
        {
            new MediaSessionCandidate("Other.Player", "Song", MediaPlaybackKind.Playing, DateTimeOffset.UtcNow),
            new MediaSessionCandidate("Spotify.exe", "Track", MediaPlaybackKind.Playing, DateTimeOffset.UtcNow.AddSeconds(-10)),
            new MediaSessionCandidate("Spotify.Store", "Paused", MediaPlaybackKind.Paused, DateTimeOffset.UtcNow),
        };

        var selected = MediaSessionSelector.Select(sessions, ["Spotify"]);

        Assert.Equal("Spotify.exe", selected?.SourceApplicationId);
    }

    [Fact]
    public void SelectUsesMostRecentSpotifyWhenNonePlaying()
    {
        var old = DateTimeOffset.Parse("2026-05-28T10:00:00Z");
        var recent = DateTimeOffset.Parse("2026-05-28T10:10:00Z");

        var selected = MediaSessionSelector.Select([
            new MediaSessionCandidate("Spotify.exe", "Old", MediaPlaybackKind.Paused, old),
            new MediaSessionCandidate("Spotify.Store", "Recent", MediaPlaybackKind.Stopped, recent),
        ], ["Spotify"]);

        Assert.Equal("Spotify.Store", selected?.SourceApplicationId);
    }

    [Fact]
    public void SelectReturnsNullWhenSpotifyIsAbsent()
    {
        var selected = MediaSessionSelector.Select([
            new MediaSessionCandidate("Browser", "Video", MediaPlaybackKind.Playing, DateTimeOffset.UtcNow),
        ], ["Spotify"]);

        Assert.Null(selected);
    }
}
