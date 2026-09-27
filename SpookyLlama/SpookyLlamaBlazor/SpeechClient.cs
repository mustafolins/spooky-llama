using System.Net.Http.Json;

namespace SpookyLlamaBlazor;

public sealed class SpeechClient(HttpClient httpClient) : IDisposable
{
    public async Task<byte[]> SynthesizeAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "speech",
            new { Text = text },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}