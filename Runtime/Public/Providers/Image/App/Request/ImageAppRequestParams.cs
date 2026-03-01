using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Parameters for sending image requests to app-based providers.
    /// </summary>
    public class ImageAppRequestParams
    {
        public ImageRequestPayload RequestPayload { get; set; }
        public List<ImageAppRequestProviderTarget> Providers { get; set; } = new List<ImageAppRequestProviderTarget>();
    }
}
