using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a unified chat response from a provider.
    /// </summary>
    public partial class ChatResponse
    {
        public string Id { get; set; }
        public string Model { get; set; }
        public string Content { get; set; }
        public ChatResponseUsageInfo Usage { get; set; }
        public string FinishReason { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> RawResponse { get; set; }
        public ChatProviderType ProviderType { get; set; } = ChatProviderType.NONE;

        public OpenAIChatResponseData OpenAIData { get; set; }
        public GoogleChatResponseData GoogleData { get; set; }
        public AnthropicChatResponseData AnthropicData { get; set; }

        /// <summary>
        /// Creates an error response with the provided message.
        /// </summary>
        /// <param name="errorMessage_">Error message.</param>
        /// <returns>Error response.</returns>
        public static ChatResponse FromError(string errorMessage_)
        {
            return FromErrorInternal(errorMessage_);
        }
    }
}
