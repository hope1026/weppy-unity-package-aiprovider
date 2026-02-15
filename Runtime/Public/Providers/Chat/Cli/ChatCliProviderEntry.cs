namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Represents a configured CLI chat provider entry.
    /// </summary>
    public class ChatCliProviderEntry
    {
        public ChatCliProviderType ProviderType { get; set; }
        public ChatCliProviderSettings Settings { get; set; }
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creates an empty provider entry.
        /// </summary>
        public ChatCliProviderEntry() { }

        /// <summary>
        /// Creates a provider entry with type and settings.
        /// </summary>
        /// <param name="providerType_">Provider type.</param>
        /// <param name="settings_">Provider settings.</param>
        public ChatCliProviderEntry(ChatCliProviderType providerType_, ChatCliProviderSettings settings_)
        {
            ProviderType = providerType_;
            Settings = settings_;
        }
    }
}
