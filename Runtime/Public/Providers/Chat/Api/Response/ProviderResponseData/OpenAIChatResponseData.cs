
namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// OpenAI-specific response metadata for chat responses.
    /// </summary>
    public class OpenAIChatResponseData
    {
        public string SystemFingerprint { get; set; }
        public string ServiceTier { get; set; }
    }
}
