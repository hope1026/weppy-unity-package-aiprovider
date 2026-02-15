using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Represents a unified chat response from a CLI provider.
    /// </summary>
    public partial class ChatCliResponse
    {
        public string Id { get; set; }
        public string Model { get; set; }
        public string Content { get; set; }
        public ChatCliResponseUsageInfo Usage { get; set; }
        public string FinishReason { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> RawResponse { get; set; }
        public ChatCliProviderType ProviderType { get; set; } = ChatCliProviderType.NONE;

        /// <summary>
        /// Creates an error response with the provided message.
        /// </summary>
        /// <param name="errorMessage_">Error message.</param>
        /// <returns>Error response.</returns>
        public static ChatCliResponse FromError(string errorMessage_)
        {
            return FromErrorInternal(errorMessage_);
        }
    }
}
