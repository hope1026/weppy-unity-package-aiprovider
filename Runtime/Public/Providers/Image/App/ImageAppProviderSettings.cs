using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Configuration options for app-based image generation providers.
    /// </summary>
    public class ImageAppProviderSettings
    {
        public string ApiKey { get; set; }
        public string DefaultModel { get; set; }
        public int TimeoutSeconds { get; set; } = 180;
        public bool UseApiKey { get; set; }
        public string AppExecutablePath { get; set; }
        public string NodeExecutablePath { get; set; }
        public string AppArguments { get; set; }
        public string AppWorkingDirectory { get; set; }
        public Dictionary<string, string> AppEnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates an empty settings instance.
        /// </summary>
        public ImageAppProviderSettings()
        {
        }

        /// <summary>
        /// Creates settings with an API key.
        /// </summary>
        /// <param name="apiKey_">Provider API key.</param>
        public ImageAppProviderSettings(string apiKey_)
        {
            ApiKey = apiKey_;
        }
    }
}
