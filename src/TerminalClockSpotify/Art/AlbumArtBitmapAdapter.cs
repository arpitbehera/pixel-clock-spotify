using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TerminalClockSpotify.Logging;

namespace TerminalClockSpotify.Art;

public sealed class AlbumArtBitmapAdapter : IArtworkImageProvider
{
    private readonly RollingFileLogger _logger;
    private readonly ArtworkCache<object> _cache;
    private readonly HashSet<string> _loggedDecodeFailures = [];

    public AlbumArtBitmapAdapter(RollingFileLogger logger)
    {
        _logger = logger;
        _cache = new ArtworkCache<object>(Decode);
    }

    public object? GetArtworkImage(byte[]? thumbnailBytes) => _cache.GetOrRender(thumbnailBytes);

    private object? Decode(byte[] thumbnailBytes)
    {
        try
        {
            using var stream = new MemoryStream(thumbnailBytes);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            var source = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var stride = source.PixelWidth * 4;
            var sourceBytes = new byte[stride * source.PixelHeight];
            source.CopyPixels(sourceBytes, stride, 0);

            var pixels = new Bgra32[source.PixelWidth * source.PixelHeight];
            for (var index = 0; index < pixels.Length; index++)
            {
                var offset = index * 4;
                pixels[index] = new Bgra32(
                    sourceBytes[offset],
                    sourceBytes[offset + 1],
                    sourceBytes[offset + 2],
                    sourceBytes[offset + 3]);
            }

            var pixelated = PixelArtRenderer.Downsample(new BgraImage(source.PixelWidth, source.PixelHeight, pixels), 32, 32);
            var renderedBytes = new byte[32 * 32 * 4];
            for (var index = 0; index < pixelated.Pixels.Count; index++)
            {
                var offset = index * 4;
                var pixel = pixelated.Pixels[index];
                renderedBytes[offset] = pixel.B;
                renderedBytes[offset + 1] = pixel.G;
                renderedBytes[offset + 2] = pixel.R;
                renderedBytes[offset + 3] = pixel.A;
            }

            var bitmap = BitmapSource.Create(32, 32, 96, 96, PixelFormats.Bgra32, null, renderedBytes, 32 * 4);
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception exception)
        {
            var hash = Convert.ToHexString(SHA256.HashData(thumbnailBytes));
            if (_loggedDecodeFailures.Add(hash))
                _logger.Warn($"Failed to decode media thumbnail {hash}: {exception.Message}");
            return null;
        }
    }
}
