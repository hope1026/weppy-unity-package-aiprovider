using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Configuration options for CLI chat providers.
    /// </summary>
    public class ChatCliProviderSettings
    {
        public string ApiKey { get; set; }
        public string DefaultModel { get; set; }
        public int TimeoutSeconds { get; set; } = 120;
        public bool UseApiKey { get; set; }
        public string CliExecutablePath { get; set; }
        public string CliArguments { get; set; }
        public string CliWorkingDirectory { get; set; }
        public string NodeExecutablePath { get; set; }
        public Dictionary<string, string> CliEnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates an empty settings instance.
        /// </summary>
        public ChatCliProviderSettings() { }

        /// <summary>
        /// Creates settings with an API key.
        /// </summary>
        /// <param name="apiKey_">Provider API key.</param>
        public ChatCliProviderSettings(string apiKey_)
        {
            ApiKey = apiKey_;
        }
    }
}
