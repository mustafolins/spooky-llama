using SpookyLlamaCommon;

public class FinalChatWord : ChatResponse
{
    public string done_reason { get; set; }
    public long[] context { get; set; }
    public long total_duration { get; set; }
    public long load_duration { get; set; }
    public long prompt_eval_count { get; set; }
    public long prompt_eval_duration { get; set; }
    public long eval_count { get; set; }
    public long eval_duration { get; set; }
}
