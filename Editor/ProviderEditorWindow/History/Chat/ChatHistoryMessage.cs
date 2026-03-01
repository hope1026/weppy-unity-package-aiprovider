namespace Weppy.AIProvider.Editor
{
    [System.Serializable]
    public class ChatHistoryMessage
    {
        public ChatHistoryMessageType MessageType;
        public string ProviderName;
        public string Content;
        public string ErrorMessage;
        public string Timestamp;
        public bool IsSuccess;
        public int AttachmentCount;
        public bool HasUsage;
        public int PromptTokens;
        public int CompletionTokens;
        public int TotalTokens;
        public bool IsStreaming;
    }
}
