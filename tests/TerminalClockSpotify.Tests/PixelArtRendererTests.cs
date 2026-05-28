using TerminalClockSpotify.Art;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class PixelArtRendererTests
{
    [Fact]
    public void DownsampleAveragesSourceCells()
    {
        var image = new BgraImage(2, 2, [
            new Bgra32(0, 0, 255, 255), new Bgra32(0, 255, 0, 255),
            new Bgra32(255, 0, 0, 255), new Bgra32(255, 255, 255, 255),
        ]);

        var result = PixelArtRenderer.Downsample(image, 1, 1);

        Assert.Equal(new Bgra32(127, 127, 127, 255), result.Pixels[0]);
    }

    [Fact]
    public void UpscaleUsesNearestNeighbor()
    {
        var image = new BgraImage(1, 1, [new Bgra32(10, 20, 30, 255)]);

        var result = PixelArtRenderer.UpscaleNearest(image, 2, 2);

        Assert.All(result.Pixels, pixel => Assert.Equal(new Bgra32(10, 20, 30, 255), pixel));
    }
}
