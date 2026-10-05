
using System.Net;

namespace SpookyLlamaVideoService;

public sealed class VideoService(
    IHttpClientFactory httpClientFactory,
    IVideoComposer videoComposer) : IVideoService
{
    public async Task<byte[]> GenerateVideoAsync(
        CancellationToken cancellationToken = default)
    {
        using var speechClient = httpClientFactory.CreateClient("speech");
        using var imageClient = httpClientFactory.CreateClient("image");
        var audioTask = GetLatestMediaAsync(
            speechClient,
            "speech/latest",
            "speech",
            cancellationToken);
        var imageTask = GetLatestMediaAsync(
            imageClient,
            "generate/latest",
            "image",
            cancellationToken);

        await Task.WhenAll(audioTask, imageTask);
        return await videoComposer.ComposeAsync(
            await imageTask,
            await audioTask,
            cancellationToken);
    }

    private static async Task<byte[]> GetLatestMediaAsync(
        HttpClient client,
        string requestUri,
        string mediaKind,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new LatestMediaUnavailableException(mediaKind);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}