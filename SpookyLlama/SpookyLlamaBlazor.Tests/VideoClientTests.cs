using System.Net;
using NUnit.Framework;

namespace SpookyLlamaBlazor.Tests;

public sealed class VideoClientTests
{
    [Test]
    public async Task GenerateAsync_PostsToVideoEndpointAndReturnsMp4()
    {
        byte[] expectedVideo = [1, 2, 3, 4];
        HttpMethod? requestMethod = null;
        string? requestPath = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requestMethod = request.Method;
            requestPath = request.RequestUri?.AbsolutePath;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedVideo)
            };
        }))
        {
            BaseAddress = new Uri("http://video/")
        };
        using var videoClient = new VideoClient(httpClient);

        var video = await videoClient.GenerateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(requestMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(requestPath, Is.EqualTo("/video"));
            Assert.That(video, Is.EqualTo(expectedVideo));
        });
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}