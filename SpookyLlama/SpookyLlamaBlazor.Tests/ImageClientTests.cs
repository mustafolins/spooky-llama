using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace SpookyLlamaBlazor.Tests;

public sealed class ImageClientTests
{
    [Test]
    public async Task GenerateAsync_TruncatesPromptToServiceLimit()
    {
        string? sentPrompt = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(async request =>
        {
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            sentPrompt = payload.RootElement.GetProperty("prompt").GetString();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            };
        }))
        {
            BaseAddress = new Uri("http://image/")
        };
        using var imageClient = new ImageClient(httpClient);

        await imageClient.GenerateAsync(new string('a', 501));

        Assert.That(sentPrompt, Has.Length.EqualTo(500));
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responseFactory(request);
    }
}