using TerminalClockSpotify.Art;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class ArtworkCacheTests
{
    [Fact]
    public void ReusesRenderedArtworkWhenThumbnailContentIsUnchanged()
    {
        var renderCalls = 0;
        var cache = new ArtworkCache<object>(_ =>
        {
            renderCalls++;
            return new object();
        });

        var first = cache.GetOrRender([1, 2, 3]);
        var second = cache.GetOrRender([1, 2, 3]);

        Assert.Same(first, second);
        Assert.Equal(1, renderCalls);
    }

    [Fact]
    public void MissingThumbnailClearsCachedArtwork()
    {
        var cache = new ArtworkCache<object>(_ => new object());

        Assert.NotNull(cache.GetOrRender([1, 2, 3]));

        Assert.Null(cache.GetOrRender(null));
        Assert.Null(cache.GetOrRender(null));
    }
}
