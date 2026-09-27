using System.Text;
using System.Text.Json;

namespace SpookyLlamaCommon;

public class SpookyLlamaManager
{
    private const string DefaultModel = "llama3.2";
    private static readonly HttpClient DefaultOllamaClient = new()
    {
        BaseAddress = new Uri("http://localhost:11434/")
    };

    public static Task<string> GetSpookyLlamaResponseAsync(string prompt, List<long> context)
    {
        return GetSpookyLlamaResponseAsync(prompt, context, DefaultOllamaClient, DefaultModel);
    }

    public static async Task<string> GetSpookyLlamaResponseAsync(
        string prompt,
        List<long> context,
        HttpClient ollamaClient,
        string modelName)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return string.Empty;
        }

        var responseText = new StringBuilder();
        await foreach (var chatWord in GetChatResponse(prompt, context, ollamaClient, modelName))
        {
            responseText.Append(chatWord.response);
            Console.Write(chatWord.response);
        }

        return responseText.ToString();
    }

    public static async Task RunSpookyLlamaAsync()
    {
        // Prompt the user for input
        Console.WriteLine("Welcome to SpookyLlama! Type your prompt and press Enter to get a response. Type nothing and press Enter to exit.");

        var prompt = Console.ReadLine();
        var context = new List<long>();
        while (!string.IsNullOrEmpty(prompt))
        {
            Console.WriteLine("SpookyLlama is thinking...\n");
            await GetSpookyLlamaResponseAsync(prompt, context);

            Console.WriteLine("\n\nSpookyLlama has finished responding.\n");
            Console.WriteLine("\n\t\t---\t\t");
            prompt = Console.ReadLine();
        }
    }

    private static async IAsyncEnumerable<ChatResponse> GetChatResponse(
        string prompt,
        List<long> context,
        HttpClient ollamaClient,
        string modelName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/generate");
        var content = new StringContent(
            JsonSerializer.Serialize(new ChatRequest
            {
                model = modelName,
                prompt = "This is a program called \"SpookyLlama\" your mission is to always respond " +
                    "in a spooky and creepy fashion to the user's prompt (think horror film responses)." +
                    "  Please avoid any non-spooky responses and try to limit it to things that could be " +
                    "pronounced by a text-to-speech engines. So avoid stuff like \"*whsipers*\" or \"*muwahaha*\"" +
                    "The user's prompt is as follows: " + prompt,
                context = [.. context]
            }),
            null,
            "application/json");
        request.Content = content;

        // Send the request and get the response
        using var response = await ollamaClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        // Read the response line by line and yield return each chat word as it arrives
        while (!reader.EndOfStream)
        {
            // Read a line from the response stream
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                // Deserialize the line into a ChatResponse object
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(line);
                if (chatResponse != null && !string.IsNullOrWhiteSpace(chatResponse.response))
                {
                    yield return chatResponse;
                }
                // If the chat response indicates it's done, update the context
                else if (chatResponse != null && chatResponse.done)
                {
                    var finalChatWord = JsonSerializer.Deserialize<FinalChatWord>(line);
                    if (finalChatWord != null)
                    {
                        context.Clear();
                        context.AddRange(finalChatWord.context);
                    }
                }
            }
        }
    }

}
