using System.Text;
using System.Text.Json;
using KokoroSharp;
using KokoroSharp.Utilities;

public class Program
{
    private static async Task Main(string[] args)
    {
        // Load the TTS model
        var tts = KokoroTTS.LoadModel();

        // Prompt the user for input
        Console.WriteLine("Welcome to SpookyLlama! Type your prompt and press Enter to get a response. Type nothing and press Enter to exit.");

        var prompt = Console.ReadLine();
        var context = new List<long>();
        while (!string.IsNullOrEmpty(prompt))
        {
            // If the prompt is null or empty, exit the program
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }

            Console.WriteLine("SpookyLlama is thinking...\n");

            // Get the chat response from the local LLaMA 3.2 API
            IEnumerable<ChatResponse?> chatWords = await GetChatResponse(prompt, context);

            // Speak the chat response using Kokoro TTS
            SpeakChatWords(tts, chatWords);
            Console.WriteLine("\n\t\t---\t\t");
            prompt = Console.ReadLine();
        }
    }

    private static async Task<IEnumerable<ChatResponse?>> GetChatResponse(string prompt, List<long> context)
    {
        // Call the local LLaMA 3.2 API locally hosted at http://localhost:11434/api/generate
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate");
        var content = new StringContent(
            JsonSerializer.Serialize(new ChatRequest 
            { 
                model = "llama3.2", 
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
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        var chatResponses = responseBody
            .Split("}\n")
            .SkipLast(2)
            .Select(str => JsonSerializer.Deserialize<ChatResponse>(str + "}"));

        var finalChatWord = JsonSerializer.Deserialize<FinalChatWord>(
            responseBody.Split("}\n").Skip(chatResponses.Count()).Take(1).First() + "}");
        if (finalChatWord != null)
        {
            context.Clear();
            context.AddRange(finalChatWord.context);
        }

        // Return the deserialized chat words
        return chatResponses;
    }

    private static void SpeakChatWords(KokoroTTS tts, IEnumerable<ChatResponse?>? chatWords)
    {
        // If chatWords is null, return early
        if (chatWords == null) return;

        // Initialize the speech synthesizer
        var voice1 = KokoroVoiceManager.GetVoice("af_nicole");
        var voice2 = KokoroVoiceManager.GetVoice("am_echo");

        // Build the full phrase from the chat words and speak it
        var phraseBuilder = BuildPhraseFromChatWords(chatWords);

        // Speak the full phrase
        var doneSpeaking = false;
        var synthesisHandle = tts.SpeakFast(
            phraseBuilder.ToString(),
            KokoroVoiceManager.Mix(
                [(voice1, 10.0f),
                (voice2, 3.0f)])
            );
        synthesisHandle.OnSpeechCompleted += (s) => doneSpeaking = true;

        // Wait for the synthesis to complete
        while (!doneSpeaking)
            Thread.Sleep(1_000); // Wait for a second to ensure the speech is completed before exiting
    }

    private static StringBuilder BuildPhraseFromChatWords(IEnumerable<ChatResponse?> chatWords)
    {
        var phraseBuilder = new StringBuilder();
        foreach (var chatWord in chatWords)
        {
            if (chatWord != null && !string.IsNullOrWhiteSpace(chatWord.response))
            {
                phraseBuilder.Append(chatWord.response);
                Console.Write(chatWord.response);
            }
        }

        return phraseBuilder;
    }
}