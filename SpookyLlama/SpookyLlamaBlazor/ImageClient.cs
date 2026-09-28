using System.Net.Http.Json;

namespace SpookyLlamaBlazor;

public sealed record GeneratedImage(byte[] Content, string? Model, string? Seed);

public sealed class ImageClient(HttpClient httpClient) : IDisposable
{
    private const int MaxPromptLength = 500;

    public async Task<GeneratedImage> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var requestPrompt = TruncatePrompt(prompt);
        using var response = await httpClient.PostAsJsonAsync(
            "generate",
            new { Prompt = requestPrompt },
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

    private static string TruncatePrompt(string prompt)
    {
        if (prompt.Length <= MaxPromptLength)
        {
            return prompt;
        }

        var length = char.IsHighSurrogate(prompt[MaxPromptLength - 1])
            ? MaxPromptLength - 1
            : MaxPromptLength;
        return prompt[..length];
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}