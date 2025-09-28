namespace SpookyLlamaCommon;

public class ChatRequest
{
    public string model { get; set; }
    public string prompt { get; set; }
    public long[] context { get; set; } = [];
}