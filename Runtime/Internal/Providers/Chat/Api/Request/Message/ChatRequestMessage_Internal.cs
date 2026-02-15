using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    public partial class ChatRequestMessage
    {
        public ChatRequestMessage() { }

        public ChatRequestMessage(ChatRequestMessageRoleType requestMessageRoleType_, string content_)
        {
            RequestMessageRoleType = requestMessageRoleType_;
            Content = content_;
        }

        public ChatRequestMessage(string role_, string content_)
        {
            RequestMessageRoleType = ParseRoleInternal(role_);
            Content = content_;
        }

        public ChatRequestMessage(ChatRequestMessageRoleType requestMessageRoleType_, string content_, List<ChatRequestMessageContent> multiContent_)
        {
            RequestMessageRoleType = requestMessageRoleType_;
            Content = content_;
            MultiContent = multiContent_;
        }

        private static ChatRequestMessageRoleType ParseRoleInternal(string role_)
        {
            return role_?.ToLowerInvariant() switch
            {
                "system" => ChatRequestMessageRoleType.SYSTEM,
                "user" => ChatRequestMessageRoleType.USER,
                "assistant" => ChatRequestMessageRoleType.ASSISTANT,
                _ => ChatRequestMessageRoleType.USER
            };
        }

        private string GetRoleStringInternal()
        {
            return RequestMessageRoleType switch
            {
                ChatRequestMessageRoleType.SYSTEM => "system",
                ChatRequestMessageRoleType.USER => "user",
                ChatRequestMessageRoleType.ASSISTANT => "assistant",
                _ => "user"
            };
        }
    }
}
