namespace Weppy.AIProvider
{
    /// <summary>
    /// OpenRouter-specific request options for image generation.
    /// </summary>
    public class OpenRouterImageRequestOptions
    {
        public string Size { get; set; }
        public string Quality { get; set; }
        public string Style { get; set; }
        public string ResponseFormat { get; set; } = "b64_json";
        public string HttpReferer { get; set; }
        public string AppTitle { get; set; }
    }
}
