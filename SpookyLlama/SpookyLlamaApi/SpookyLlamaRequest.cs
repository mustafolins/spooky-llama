public class SpookyLlamaRequest
{
    public string Prompt { get; set; }
    public long[] Context { get; set; } = [];
}