namespace Weppy.AIProvider
{
    /// <summary>
    /// Image content data for a chat message.
    /// </summary>
    public class ChatRequestMessageImageContent
    {
        public string Base64Data { get; set; }
        public string Url { get; set; }
        public string MediaType { get; set; } = "image/png";
    }
}
