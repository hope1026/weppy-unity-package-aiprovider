namespace Weppy.AIProvider
{
    /// <summary>
    /// OpenAI-specific request options for image generation.
    /// </summary>
    public class OpenAIImageRequestOptions
    {
        public string Size { get; set; }
        public string Quality { get; set; }
        public string Style { get; set; }
        public string ResponseFormat { get; set; } = "b64_json";
    }
}
