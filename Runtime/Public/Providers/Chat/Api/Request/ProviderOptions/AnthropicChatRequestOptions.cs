using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Anthropic-specific request options for chat payloads.
    /// </summary>
    public class AnthropicChatRequestOptions
    {
        public int? TopK { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
