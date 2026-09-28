using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace SpookyLlamaBlazor.Pages
{
    public partial class Home : IAsyncDisposable
    {
        private const string DefaultPrompt = "Tell me a spooky story";

        [Inject]
        private HttpClient HttpClient { get; set; } = default!;

        [Inject]
        private SpeechClient SpeechClient { get; set; } = default!;

        [Inject]
        private ImageClient ImageClient { get; set; } = default!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        private ILogger<Home> Logger { get; set; } = default!;

        private ElementReference audioPlayer;
        private ElementReference generatedImage;
        private IJSObjectReference? audioModule;
        private IJSObjectReference? imageModule;

        public string Prompt { get; set; } = DefaultPrompt;
        public string LatestResponse { get; set; } = string.Empty;
        public List<string> Responses { get; set; } = [];
        public bool IsGenerating { get; set; }
        public bool IsGeneratingImage { get; set; }
        public bool HasGeneratedImage { get; set; }
        public string ImagePrompt { get; set; } = DefaultPrompt;
        public string ImageMetadata { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        private async Task GenerateAndPlay()
        {
            IsGenerating = true;
            ErrorMessage = string.Empty;
            try
            {
                using var response = await HttpClient.PostAsJsonAsync("api/spookyllama", new { Prompt });
                response.EnsureSuccessStatusCode();

                LatestResponse = await response.Content.ReadFromJsonAsync<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(LatestResponse))
                {
                    return;
                }

                Responses.Add(LatestResponse);
                ImagePrompt = LatestResponse;
                var audio = await SpeechClient.SynthesizeAsync(LatestResponse);
                audioModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./audio.js");
                await audioModule.InvokeAsync<bool>("play", audioPlayer, audio);
            }
            catch (Exception)
            {
                ErrorMessage = "The response could not be generated or played.";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private async Task GenerateImage()
        {
            if (string.IsNullOrWhiteSpace(ImagePrompt))
            {
                return;
            }

            IsGeneratingImage = true;
            ErrorMessage = string.Empty;
            try
            {
                var image = await ImageClient.GenerateAsync(ImagePrompt);
                imageModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./image.js");
                await imageModule.InvokeVoidAsync("show", generatedImage, image.Content);
                HasGeneratedImage = true;
                ImageMetadata = $"{image.Model ?? "DreamShaper 8"} · seed {image.Seed ?? "random"}";
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Image generation failed.");
                ErrorMessage = "The image could not be generated.";
            }
            finally
            {
                IsGeneratingImage = false;
            }
        }

        private async Task GetLatestResponse()
        {
            var response = await HttpClient.GetAsync("api/spookyllama/response");
            response.EnsureSuccessStatusCode();

            LatestResponse = await response.Content.ReadFromJsonAsync<string>() ?? string.Empty;
        }

        private async Task GetAllResponses()
        {
            var response = await HttpClient.GetAsync("api/spookyllama/responses");
            response.EnsureSuccessStatusCode();

            Responses = await response.Content.ReadFromJsonAsync<List<string>>() ?? [];
        }

        private async Task ClearResponses()
        {
            var response = await HttpClient.DeleteAsync("api/spookyllama/responses");
            response.EnsureSuccessStatusCode();
            Responses.Clear();
            LatestResponse = string.Empty;
            ErrorMessage = string.Empty;
        }

        private async Task ClearContext()
        {
            var response = await HttpClient.DeleteAsync("api/spookyllama/context");
            response.EnsureSuccessStatusCode();
            Responses.Clear();
            LatestResponse = string.Empty;
            ErrorMessage = string.Empty;
        }

        public async ValueTask DisposeAsync()
        {
            if (audioModule is not null)
            {
                await audioModule.InvokeVoidAsync("dispose");
                await audioModule.DisposeAsync();
            }

            if (imageModule is not null)
            {
                await imageModule.InvokeVoidAsync("dispose");
                await imageModule.DisposeAsync();
            }
        }
    }
}