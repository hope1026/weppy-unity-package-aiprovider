namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Anthropic-specific response metadata for chat responses.
    /// </summary>
    public class AnthropicChatResponseData
    {
        public string StopSequence { get; set; }
        public int? CacheCreationInputTokens { get; set; }
        public int? CacheReadInputTokens { get; set; }
    }
}
