namespace TerminalClockSpotify.Media;

public interface IMediaSessionService
{
    Task<NowPlayingState> GetNowPlayingAsync(IReadOnlyList<string> spotifyTokens, CancellationToken cancellationToken);
}
