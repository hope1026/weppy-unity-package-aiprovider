namespace Weppy.AIProvider
{
    /// <summary>
    /// Document content data for a chat message.
    /// </summary>
    public class ChatRequestMessageDocumentContent
    {
        public string Base64Data { get; set; }
        public string TextContent { get; set; }
        public string MediaType { get; set; }
        public string FileName { get; set; }
    }
}
