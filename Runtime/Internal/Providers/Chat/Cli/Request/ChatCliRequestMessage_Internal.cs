namespace Weppy.AIProvider
{
    public partial class ChatCliRequestMessage
    {
        public ChatCliRequestMessage() { }

        public ChatCliRequestMessage(ChatCliRequestMessageRoleType requestMessageRoleType_, string content_)
        {
            RequestMessageRoleType = requestMessageRoleType_;
            Content = content_;
        }

        public ChatCliRequestMessage(string role_, string content_)
        {
            RequestMessageRoleType = ParseRoleInternal(role_);
            Content = content_;
        }

        private static ChatCliRequestMessageRoleType ParseRoleInternal(string role_)
        {
            return role_?.ToLowerInvariant() switch
            {
                "system" => ChatCliRequestMessageRoleType.SYSTEM,
                "user" => ChatCliRequestMessageRoleType.USER,
                "assistant" => ChatCliRequestMessageRoleType.ASSISTANT,
                _ => ChatCliRequestMessageRoleType.USER
            };
        }

        private string GetRoleStringInternal()
        {
            return RequestMessageRoleType switch
            {
                ChatCliRequestMessageRoleType.SYSTEM => "system",
                ChatCliRequestMessageRoleType.USER => "user",
                ChatCliRequestMessageRoleType.ASSISTANT => "assistant",
                _ => "user"
            };
        }
    }
}
