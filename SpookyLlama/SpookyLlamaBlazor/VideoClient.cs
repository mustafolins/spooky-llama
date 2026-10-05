namespace SpookyLlamaBlazor;

public sealed class VideoClient(HttpClient httpClient) : IDisposable
{
    public async Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "video");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}