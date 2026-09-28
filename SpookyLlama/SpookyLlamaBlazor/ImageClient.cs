using System.Net.Http.Json;

namespace SpookyLlamaBlazor;

public sealed record GeneratedImage(byte[] Content, string? Model, string? Seed);

public sealed class ImageClient(HttpClient httpClient) : IDisposable
{
    public async Task<GeneratedImage> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "generate",
            new { Prompt = prompt },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return new GeneratedImage(
            await response.Content.ReadAsByteArrayAsync(cancellationToken),
            GetHeader(response, "X-Image-Model"),
            GetHeader(response, "X-Image-Seed"));
    }

    private static string? GetHeader(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values)
            ? values.FirstOrDefault()
            : null;
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}