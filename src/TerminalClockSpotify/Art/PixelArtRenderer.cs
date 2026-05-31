namespace TerminalClockSpotify.Art;

public readonly record struct Bgra32(byte B, byte G, byte R, byte A);

public sealed record BgraImage(int Width, int Height, IReadOnlyList<Bgra32> Pixels)
{
    public Bgra32 At(int x, int y) => Pixels[y * Width + x];
}

public static class PixelArtRenderer
{
    public static BgraImage CropBottom(BgraImage source, double heightRatio)
    {
        if (heightRatio <= 0)
            return source;

        var removedRows = (int)Math.Round(source.Height * heightRatio, MidpointRounding.AwayFromZero);
        var targetHeight = Math.Max(1, source.Height - removedRows);
        var pixels = source.Pixels.Take(source.Width * targetHeight).ToArray();

        return new BgraImage(source.Width, targetHeight, pixels);
    }

    public static BgraImage CropCenterSquare(BgraImage source)
    {
        var side = Math.Min(source.Width, source.Height);
        if (side == source.Width && side == source.Height)
            return source;

        var startX = (source.Width - side) / 2;
        var startY = (source.Height - side) / 2;
        var pixels = new List<Bgra32>(side * side);

        for (var y = 0; y < side; y++)
            for (var x = 0; x < side; x++)
                pixels.Add(source.At(startX + x, startY + y));

        return new BgraImage(side, side, pixels);
    }

    public static BgraImage Downsample(BgraImage source, int targetWidth, int targetHeight)
    {
        var pixels = new List<Bgra32>(targetWidth * targetHeight);

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var startX = x * source.Width / targetWidth;
                var endX = Math.Max(startX + 1, (x + 1) * source.Width / targetWidth);
                var startY = y * source.Height / targetHeight;
                var endY = Math.Max(startY + 1, (y + 1) * source.Height / targetHeight);
                var count = 0;
                var b = 0;
                var g = 0;
                var r = 0;
                var a = 0;

                for (var yy = startY; yy < endY; yy++)
                {
                    for (var xx = startX; xx < endX; xx++)
                    {
                        var pixel = source.At(xx, yy);
                        b += pixel.B;
                        g += pixel.G;
                        r += pixel.R;
                        a += pixel.A;
                        count++;
                    }
                }

                pixels.Add(new Bgra32((byte)(b / count), (byte)(g / count), (byte)(r / count), (byte)(a / count)));
            }
        }

        return new BgraImage(targetWidth, targetHeight, pixels);
    }

    public static BgraImage UpscaleNearest(BgraImage source, int targetWidth, int targetHeight)
    {
        var pixels = new List<Bgra32>(targetWidth * targetHeight);

        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                pixels.Add(source.At(x * source.Width / targetWidth, y * source.Height / targetHeight));
            }
        }

        return new BgraImage(targetWidth, targetHeight, pixels);
    }
}
