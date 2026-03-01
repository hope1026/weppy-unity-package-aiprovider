using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// OpenRouter-specific request options for chat payloads.
    /// </summary>
    public class OpenRouterChatRequestOptions
    {
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
        public int? TopK { get; set; }
        public int? Seed { get; set; }
        public string ResponseFormat { get; set; }
        public Dictionary<string, int> LogitBias { get; set; }
        public string User { get; set; }
        public int? N { get; set; }
        public string HttpReferer { get; set; }
        public string AppTitle { get; set; }
        public List<string> Transforms { get; set; }
        public List<string> Models { get; set; }
        public string Route { get; set; }
        public object Provider { get; set; }
    }
}
