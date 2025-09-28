using System.Text;
using System.Text.Json;
using KokoroSharp;
using KokoroSharp.Core;

public class Program
{
    private static object LockObject = new();
    private static KokoroVoice? voice;

    private static async Task Main(string[] args)
    {
        // Load the TTS model
        var tts = KokoroTTS.LoadModel();

        // Initialize the speech synthesizer
        var voice1 = KokoroVoiceManager.GetVoice("af_nicole");
        var voice2 = KokoroVoiceManager.GetVoice("am_echo");
        voice = KokoroVoiceManager.Mix(
                    [(voice1, 10.0f),
                (voice2, 3.0f)]);

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
            var chatWordsList = new List<ChatResponse>();
            await foreach (var chatWord in GetChatResponse(prompt, context))
            {
                // Add the chat word to the list
                chatWordsList.Add(chatWord);
                // If the chat word is a punctuation mark, speak the current phrases
                if (chatWord != null && 
                    (chatWord.response == "." || chatWord.response == "!" || chatWord.response == "?"))
                {
                    SpeakChatWords(tts, chatWordsList);
                    chatWordsList.Clear();
                }
            }

            // Speak any remaining chat words after the response is complete
            SpeakChatWords(tts, chatWordsList);

            Console.WriteLine("\n\nSpookyLlama has finished responding.\n");
            Console.WriteLine("\n\t\t---\t\t");
            prompt = Console.ReadLine();
        }
    }

    private static async IAsyncEnumerable<ChatResponse> GetChatResponse(string prompt, List<long> context)
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

    private static void SpeakChatWords(KokoroTTS tts, IEnumerable<ChatResponse?>? chatWords)
    {
        // If chatWords is null, return early
        if (chatWords == null) return;

        lock (LockObject)
        {
            // Build the full phrase from the chat words and speak it
            var phraseBuilder = BuildPhraseFromChatWords(chatWords);

            if (phraseBuilder.Length == 0) return; // Nothing to speak

            // Speak the full phrase
            var synthesisHandle = tts.SpeakFast(
                phraseBuilder.ToString(),
                voice
                );
            var doneSpeaking = false;
            synthesisHandle.OnSpeechCompleted += (s) => doneSpeaking = true;

            // Wait for the synthesis to complete
            while (!doneSpeaking)
                Thread.Sleep(5000); // Wait for a second to ensure the speech is completed before exiting 
        }
    }

    private static StringBuilder BuildPhraseFromChatWords(IEnumerable<ChatResponse?> chatWords)
    {
        // Build the full phrase from the chat words
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