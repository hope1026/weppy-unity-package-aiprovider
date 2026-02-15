using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Represents a single chat message in a request.
    /// </summary>
    public partial class ChatRequestMessage
    {
        public ChatRequestMessageRoleType RequestMessageRoleType { get; set; }
        public string Content { get; set; }
        public List<ChatRequestMessageContent> MultiContent { get; set; }

        public OpenAIChatRequestMessageOptions OpenAIOptions { get; set; }

        /// <summary>
        /// Parses a role string into a role enum.
        /// </summary>
        /// <param name="role_">Role string.</param>
        /// <returns>Parsed role type.</returns>
        public static ChatRequestMessageRoleType ParseRole(string role_)
        {
            return ParseRoleInternal(role_);
        }

        /// <summary>
        /// Gets the role string for this message.
        /// </summary>
        /// <returns>Role string.</returns>
        public string GetRoleString()
        {
            return GetRoleStringInternal();
        }

        /// <summary>
        /// Sets OpenAI message options for this message.
        /// </summary>
        /// <param name="options_">OpenAI message options.</param>
        /// <returns>The updated message.</returns>
        public ChatRequestMessage WithOpenAIData(OpenAIChatRequestMessageOptions options_)
        {
            OpenAIOptions = options_;
            return this;
        }

        /// <summary>
        /// Sets the OpenAI message name.
        /// </summary>
        /// <param name="name_">OpenAI message name.</param>
        /// <returns>The updated message.</returns>
        public ChatRequestMessage WithOpenAIName(string name_)
        {
            OpenAIOptions = new OpenAIChatRequestMessageOptions(name_);
            return this;
        }
    }
}
