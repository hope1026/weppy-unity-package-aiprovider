using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Wraps provider targets and payload for a background removal request.
    /// </summary>
    public class BgRemovalRequestParams
    {
        public List<BgRemovalRequestProviderTarget> Providers { get; set; }
        public BgRemovalRequestPayload RequestPayload { get; set; }
    }
}
