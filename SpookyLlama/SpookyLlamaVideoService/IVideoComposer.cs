namespace SpookyLlamaVideoService;

public interface IVideoComposer
{
    Task<byte[]> ComposeAsync(
        byte[] image,
        byte[] audio,
        CancellationToken cancellationToken = default);
}