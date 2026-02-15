namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Token usage information for a CLI chat response.
    /// </summary>
    public class ChatCliResponseUsageInfo
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }

        /// <summary>
        /// Creates an empty usage info instance.
        /// </summary>
        public ChatCliResponseUsageInfo() { }

        /// <summary>
        /// Creates usage info with prompt and completion tokens.
        /// </summary>
        /// <param name="promptTokens_">Prompt token count.</param>
        /// <param name="completionTokens_">Completion token count.</param>
        public ChatCliResponseUsageInfo(int promptTokens_, int completionTokens_)
        {
            PromptTokens = promptTokens_;
            CompletionTokens = completionTokens_;
            TotalTokens = promptTokens_ + completionTokens_;
        }
    }
}
