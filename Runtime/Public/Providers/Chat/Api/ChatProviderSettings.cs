using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Configuration options for chat providers.
    /// </summary>
    public class ChatProviderSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public string OrganizationId { get; set; }
        public string DefaultModel { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxRetries { get; set; } = 3;
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates an empty settings instance.
        /// </summary>
        public ChatProviderSettings()
        {
        }

        /// <summary>
        /// Creates settings with an API key.
        /// </summary>
        /// <param name="apiKey_">Provider API key.</param>
        public ChatProviderSettings(string apiKey_)
        {
            ApiKey = apiKey_;
        }

        /// <summary>
        /// Creates settings with an API key and base URL.
        /// </summary>
        /// <param name="apiKey_">Provider API key.</param>
        /// <param name="baseUrl_">Provider base URL override.</param>
        public ChatProviderSettings(string apiKey_, string baseUrl_) : this(apiKey_)
        {
            BaseUrl = baseUrl_;
        }
    }
}
