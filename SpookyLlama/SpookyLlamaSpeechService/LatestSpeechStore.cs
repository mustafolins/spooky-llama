using Microsoft.Extensions.Caching.Distributed;

namespace SpookyLlamaSpeechService;

public sealed class LatestSpeechStore(IDistributedCache cache)
{
    private const string CacheKey = "spooky-llama:media:speech:latest";

    public Task<byte[]?> GetAsync(CancellationToken cancellationToken = default)
    {
        return cache.GetAsync(CacheKey, cancellationToken);
    }

    public Task SetAsync(byte[] content, CancellationToken cancellationToken = default)
    {
        return cache.SetAsync(
            CacheKey,
            content,
            new DistributedCacheEntryOptions(),
            cancellationToken);
    }
}