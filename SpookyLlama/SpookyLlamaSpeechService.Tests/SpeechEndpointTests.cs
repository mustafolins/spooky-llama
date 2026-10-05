using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace SpookyLlamaSpeechService.Tests;

public sealed class SpeechEndpointTests
{
    [Test]
    public async Task Latest_BeforeSynthesis_ReturnsNotFound()
    {
        await using var application = new SpeechServiceApplication();
        using var client = application.CreateClient();

        using var response = await client.GetAsync("/speech/latest");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Latest_AfterSynthesis_ReturnsMostRecentWavAudio()
    {
        await using var application = new SpeechServiceApplication();
        using var client = application.CreateClient();
        using var synthesisResponse = await client.PostAsJsonAsync(
            "/speech",
            new { Text = "Beware the shadows." });
        synthesisResponse.EnsureSuccessStatusCode();

        using var response = await client.GetAsync("/speech/latest");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/wav"));
        });
        Assert.That(await response.Content.ReadAsByteArrayAsync(), Is.EqualTo(FakeSpeechSynthesizer.WavBytes));
    }

    [Test]
    public async Task Synthesize_WithText_ReturnsWavAudio()
    {
        await using var application = new SpeechServiceApplication();
        using var client = application.CreateClient();

        using var response = await client.PostAsJsonAsync("/speech", new { Text = "Beware the shadows." });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("audio/wav"));
        });
        Assert.That(await response.Content.ReadAsByteArrayAsync(), Is.EqualTo(FakeSpeechSynthesizer.WavBytes));
    }

    [Test]
    public async Task Synthesize_WithoutText_ReturnsBadRequest()
    {
        await using var application = new SpeechServiceApplication();
        using var client = application.CreateClient();

        using var response = await client.PostAsJsonAsync("/speech", new { Text = "   " });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private sealed class SpeechServiceApplication : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISpeechSynthesizer>();
                services.RemoveAll<IDistributedCache>();
                services.AddSingleton<ISpeechSynthesizer, FakeSpeechSynthesizer>();
                services.AddDistributedMemoryCache();
            });
        }
    }

    private sealed class FakeSpeechSynthesizer : ISpeechSynthesizer
    {
        public static readonly byte[] WavBytes = "RIFF-test-wave"u8.ToArray();

        public Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(WavBytes);
        }
    }
}