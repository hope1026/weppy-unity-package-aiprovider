using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Configuration options for image generation providers.
    /// </summary>
    public class ImageProviderSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public string OrganizationId { get; set; }
        public string DefaultModel { get; set; }
        public int TimeoutSeconds { get; set; } = 120;
        public int MaxRetries { get; set; } = 3;
        public Dictionary<string, string> CustomHeaders { get; set; } = new Dictionary<string, string>();
        public string CliExecutablePath { get; set; }
        public string CliArguments { get; set; }
        public string CliWorkingDirectory { get; set; }
        public Dictionary<string, string> CliEnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates an empty settings instance.
        /// </summary>
        public ImageProviderSettings() { }

        /// <summary>
        /// Creates settings with an API key.
        /// </summary>
        /// <param name="apiKey_">Provider API key.</param>
        public ImageProviderSettings(string apiKey_)
        {
            ApiKey = apiKey_;
        }

        /// <summary>
        /// Creates settings with an API key and base URL.
        /// </summary>
        /// <param name="apiKey_">Provider API key.</param>
        /// <param name="baseUrl_">Provider base URL override.</param>
        public ImageProviderSettings(string apiKey_, string baseUrl_) : this(apiKey_)
        {
            BaseUrl = baseUrl_;
        }
    }
}
