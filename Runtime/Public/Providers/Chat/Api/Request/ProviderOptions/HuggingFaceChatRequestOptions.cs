namespace Weppy.AIProvider
{
    /// <summary>
    /// HuggingFace-specific request options for chat payloads.
    /// </summary>
    public class HuggingFaceChatRequestOptions
    {
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
    }
}
