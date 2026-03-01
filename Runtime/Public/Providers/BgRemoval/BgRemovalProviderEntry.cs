namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a configured background removal provider entry.
    /// </summary>
    public partial class BgRemovalProviderEntry
    {
        public BgRemovalProviderType ProviderType { get; set; }
        public BgRemovalProviderSettings Settings { get; set; }
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creates an empty provider entry.
        /// </summary>
        public BgRemovalProviderEntry() { }

        /// <summary>
        /// Creates a provider entry with type and settings.
        /// </summary>
        /// <param name="providerType_">Provider type.</param>
        /// <param name="settings_">Provider settings.</param>
        public BgRemovalProviderEntry(BgRemovalProviderType providerType_, BgRemovalProviderSettings settings_)
        {
            InitializeInternal(providerType_, settings_);
        }
    }
}
