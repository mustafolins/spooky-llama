namespace SpookyLlamaSpeechService;

public interface ISpeechSynthesizer
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
}