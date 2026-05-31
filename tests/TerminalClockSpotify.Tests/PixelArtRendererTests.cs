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

    [Fact]
    public void CropBottomRemovesBottomRows()
    {
        var image = new BgraImage(2, 4, [
            new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
            new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
            new Bgra32(5, 0, 0, 255), new Bgra32(6, 0, 0, 255),
            new Bgra32(7, 0, 0, 255), new Bgra32(8, 0, 0, 255),
        ]);

        var result = PixelArtRenderer.CropBottom(image, 0.25);

        Assert.Equal(2, result.Width);
        Assert.Equal(3, result.Height);
        Assert.Equal([
            new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
            new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
            new Bgra32(5, 0, 0, 255), new Bgra32(6, 0, 0, 255),
        ], result.Pixels);
    }

    [Fact]
    public void CropCenterSquareTrimsWiderSidesAroundMiddleColumn()
    {
        var image = new BgraImage(4, 2, [
            new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255), new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
            new Bgra32(5, 0, 0, 255), new Bgra32(6, 0, 0, 255), new Bgra32(7, 0, 0, 255), new Bgra32(8, 0, 0, 255),
        ]);

        var result = PixelArtRenderer.CropCenterSquare(image);

        Assert.Equal(2, result.Width);
        Assert.Equal(2, result.Height);
        Assert.Equal([
            new Bgra32(2, 0, 0, 255), new Bgra32(3, 0, 0, 255),
            new Bgra32(6, 0, 0, 255), new Bgra32(7, 0, 0, 255),
        ], result.Pixels);
    }

    [Fact]
    public void CropCenterSquareLeavesSquareImageUnchanged()
    {
        var image = new BgraImage(2, 2, [
            new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
            new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
        ]);

        var result = PixelArtRenderer.CropCenterSquare(image);

        Assert.Equal(2, result.Width);
        Assert.Equal(2, result.Height);
        Assert.Equal(image.Pixels, result.Pixels);
    }

    [Fact]
    public void CropBottomLeavesAtLeastOneRow()
    {
        var image = new BgraImage(2, 2, [
            new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
            new Bgra32(3, 0, 0, 255), new Bgra32(4, 0, 0, 255),
        ]);

        var result = PixelArtRenderer.CropBottom(image, 1);

        Assert.Equal(2, result.Width);
        Assert.Equal(1, result.Height);
        Assert.Equal([
            new Bgra32(1, 0, 0, 255), new Bgra32(2, 0, 0, 255),
        ], result.Pixels);
    }
}
