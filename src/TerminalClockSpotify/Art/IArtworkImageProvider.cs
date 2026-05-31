using System.Security.Cryptography;

namespace TerminalClockSpotify.Art;

public interface IArtworkImageProvider
{
    object? GetArtworkImage(byte[]? thumbnailBytes);
}

public sealed class ArtworkCache<T>(Func<byte[], T?> render) where T : class
{
    private byte[]? _hash;
    private T? _artwork;

    public T? GetOrRender(byte[]? thumbnailBytes)
    {
        if (thumbnailBytes is null || thumbnailBytes.Length == 0)
        {
            _hash = null;
            _artwork = null;
            return null;
        }

        var hash = SHA256.HashData(thumbnailBytes);
        if (_hash is not null && hash.AsSpan().SequenceEqual(_hash))
            return _artwork;

        _hash = hash;
        _artwork = render(thumbnailBytes);
        return _artwork;
    }
}
