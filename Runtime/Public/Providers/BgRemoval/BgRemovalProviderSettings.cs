using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Configuration options for background removal providers.
    /// </summary>
    public partial class BgRemovalProviderSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public string DefaultModel { get; set; }
        public int TimeoutSeconds { get; set; } = 120;
        public int MaxRetries { get; set; } = 3;
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a deep copy of the settings.
        /// </summary>
        /// <returns>Cloned settings instance.</returns>
        public BgRemovalProviderSettings Clone()
        {
            return CloneInternal();
        }
    }
}
