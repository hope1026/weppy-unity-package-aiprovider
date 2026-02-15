using System.Collections.Generic;

namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// OpenAI-specific request options for chat payloads.
    /// </summary>
    public class OpenAIChatRequestOptions
    {
        public float? FrequencyPenalty { get; set; }
        public float? PresencePenalty { get; set; }
        public int? Seed { get; set; }
        public string ResponseFormat { get; set; }
        public Dictionary<string, int> LogitBias { get; set; }
        public string User { get; set; }
        public int? N { get; set; }
    }
}
