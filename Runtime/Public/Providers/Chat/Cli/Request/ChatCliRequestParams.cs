using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Wraps provider targets and payload for a CLI chat request.
    /// </summary>
    public class ChatCliRequestParams
    {
        public List<ChatCliRequestProviderTarget> Providers { get; set; }
        public ChatCliRequestPayload RequestPayload { get; set; }
    }
}
