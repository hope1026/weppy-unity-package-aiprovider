using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Wraps provider targets and payload for a chat request.
    /// </summary>
    public class ChatRequestParams
    {
        public List<ChatRequestProviderTarget> Providers { get; set; }
        public ChatRequestPayload RequestPayload { get; set; }
    }
}
