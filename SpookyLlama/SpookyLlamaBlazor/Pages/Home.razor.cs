using System.Text.Json;

namespace SpookyLlamaBlazor.Pages
{
    public partial class Home
    {
        public string Prompt { get; set; } = "Tell me a spooky story";
        public string LatestResponse { get; set; } = string.Empty;
        public List<string> Responses { get; set; } = [];

        private async Task GenerateAndPlay()
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7233/api/spookyllama");
            var content = new StringContent(
                $"{{\"prompt\": \"{Prompt}\"}}",
                null,
                "application/json");
            request.Content = content;

            // Send the request and get the response
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        private async Task GetLatestResponse()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://localhost:7233/api/spookyllama/response");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            LatestResponse = responseBody;
        }

        private async Task GetAllResponses()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://localhost:7233/api/spookyllama/responses");
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            Responses = JsonSerializer.Deserialize<List<string>>(responseBody) ?? [];
        }

        private async Task ClearResponses()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.DeleteAsync("https://localhost:7233/api/spookyllama/responses");
            response.EnsureSuccessStatusCode();
            Responses.Clear();
            LatestResponse = string.Empty;
        }

        private async Task ClearContext()
        {
            var httpClient = new HttpClient();
            var response = await httpClient.DeleteAsync("https://localhost:7233/api/spookyllama/context");
            response.EnsureSuccessStatusCode();
            Responses.Clear();
            LatestResponse = string.Empty;
        }
    }
}