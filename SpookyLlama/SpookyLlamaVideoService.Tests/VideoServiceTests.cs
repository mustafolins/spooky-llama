using System.Net;
using System.Text;
using NUnit.Framework;
using SpookyLlamaVideoService;

namespace SpookyLlamaVideoService.Tests;

public sealed class VideoServiceTests
{
    [Test]
    public async Task GenerateVideoAsync_UsesLatestSpeechAndImage()
    {
        byte[] audio = [1, 2, 3];
        byte[] image = [4, 5, 6];
        byte[] video = [7, 8, 9];
        var speechHandler = new StubHttpMessageHandler(_ => FileResponse(audio, "audio/wav"));
        var imageHandler = new StubHttpMessageHandler(_ => FileResponse(image, "image/png"));
        var composer = new RecordingVideoComposer(video);
        using var speechClient = CreateClient(speechHandler, "http://speech/");
        using var imageClient = CreateClient(imageHandler, "http://image/");
        var service = new VideoService(
            new StubHttpClientFactory(speechClient, imageClient),
            composer);

        var result = await service.GenerateVideoAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(video));
            Assert.That(composer.Image, Is.EqualTo(image));
            Assert.That(composer.Audio, Is.EqualTo(audio));
            Assert.That(speechHandler.RequestPath, Is.EqualTo("/speech/latest"));
            Assert.That(imageHandler.RequestPath, Is.EqualTo("/generate/latest"));
        });
    }

    [Test]
    public void GenerateVideoAsync_WithoutLatestSpeech_ReportsMissingDependency()
    {
        var speechHandler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var imageHandler = new StubHttpMessageHandler(_ => FileResponse([4, 5, 6], "image/png"));
        using var speechClient = CreateClient(speechHandler, "http://speech/");
        using var imageClient = CreateClient(imageHandler, "http://image/");
        var service = new VideoService(
            new StubHttpClientFactory(speechClient, imageClient),
            new RecordingVideoComposer([7, 8, 9]));

        var exception = Assert.ThrowsAsync<LatestMediaUnavailableException>(
            async () => await service.GenerateVideoAsync());

        Assert.That(exception!.MediaKind, Is.EqualTo("speech"));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler, string baseAddress)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseAddress)
        };
    }

    private static HttpResponseMessage FileResponse(byte[] content, string contentType)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content)
        };
        response.Content.Headers.ContentType = new(contentType);
        return response;
    }

    private sealed class StubHttpClientFactory(
        HttpClient speechClient,
        HttpClient imageClient) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return name switch
            {
                "speech" => speechClient,
                "image" => imageClient,
                _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
            };
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public string? RequestPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.AbsolutePath;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class RecordingVideoComposer(byte[] video) : IVideoComposer
    {
        public byte[]? Image { get; private set; }
        public byte[]? Audio { get; private set; }

        public Task<byte[]> ComposeAsync(
            byte[] image,
            byte[] audio,
            CancellationToken cancellationToken = default)
        {
            Image = image;
            Audio = audio;
            return Task.FromResult(video);
        }
    }
}

public sealed class FFmpegVideoComposerTests
{
    [Test]
    public async Task ComposeAsync_WithPngAndWav_ReturnsMp4()
    {
        var composer = new FFmpegVideoComposer();

        var video = await composer.ComposeAsync(CreatePng(), CreateWav());

        Assert.Multiple(() =>
        {
            Assert.That(video, Has.Length.GreaterThan(1_000));
            Assert.That(Encoding.ASCII.GetString(video, 4, 4), Is.EqualTo("ftyp"));
        });
    }

    private static byte[] CreatePng()
    {
        return Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    }

    private static byte[] CreateWav()
    {
        const int sampleRate = 8_000;
        const short channels = 1;
        const short bitsPerSample = 16;
        var samples = new byte[sampleRate * channels * bitsPerSample / 8];
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("RIFF"u8);
        writer.Write(36 + samples.Length);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write("data"u8);
        writer.Write(samples.Length);
        writer.Write(samples);
        writer.Flush();
        return stream.ToArray();
    }
}