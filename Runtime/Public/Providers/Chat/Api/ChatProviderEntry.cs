namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Represents a configured chat provider entry.
    /// </summary>
    public class ChatProviderEntry
    {
        public ChatProviderType ProviderType { get; set; }
        public ChatProviderSettings Settings { get; set; }
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creates an empty provider entry.
        /// </summary>
        public ChatProviderEntry() { }

        /// <summary>
        /// Creates a provider entry with type and settings.
        /// </summary>
        /// <param name="providerType_">Provider type.</param>
        /// <param name="settings_">Provider settings.</param>
        public ChatProviderEntry(ChatProviderType providerType_, ChatProviderSettings settings_)
        {
            ProviderType = providerType_;
            Settings = settings_;
        }
    }
}
