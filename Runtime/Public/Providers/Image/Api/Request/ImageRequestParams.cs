using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Wraps provider targets and payload for an image request.
    /// </summary>
    public class ImageRequestParams
    {
        public List<ImageRequestProviderTarget> Providers { get; set; }
        public ImageRequestPayload RequestPayload { get; set; }
    }
}
