namespace SpookyLlamaVideoService;

public interface IVideoService
{
    Task<byte[]> GenerateVideoAsync(CancellationToken cancellationToken = default);
}
