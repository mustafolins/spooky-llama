namespace SpookyLlamaVideoService;

public sealed class LatestMediaUnavailableException(string mediaKind)
    : Exception($"No generated {mediaKind} is available.")
{
    public string MediaKind { get; } = mediaKind;
}