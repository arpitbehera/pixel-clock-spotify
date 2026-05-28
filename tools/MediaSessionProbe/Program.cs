using Windows.Media.Control;

var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
var sessions = manager.GetSessions();

Console.WriteLine($"Sessions: {sessions.Count}");

foreach (var session in sessions)
{
    var media = await session.TryGetMediaPropertiesAsync();
    var playback = session.GetPlaybackInfo();
    var timeline = session.GetTimelineProperties();

    Console.WriteLine($"Source: {session.SourceAppUserModelId}");
    Console.WriteLine($"Playback: {playback.PlaybackStatus}");
    Console.WriteLine($"Title: {media.Title}");
    Console.WriteLine($"Artist: {media.Artist}");
    Console.WriteLine($"Album: {media.AlbumTitle}");
    Console.WriteLine($"Position: {timeline.Position}");
    Console.WriteLine($"Duration: {timeline.EndTime}");
    Console.WriteLine($"Thumbnail: {(media.Thumbnail is null ? "none" : "available")}");
    Console.WriteLine();
}
