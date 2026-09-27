using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;

namespace SpookyLlamaSpeechService.Tests;

public sealed class SpeechEndpointTests
{
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
                services.AddSingleton<ISpeechSynthesizer, FakeSpeechSynthesizer>();
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