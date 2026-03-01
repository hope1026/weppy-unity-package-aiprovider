namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a configured image provider entry.
    /// </summary>
    public class ImageProviderEntry
    {
        public ImageProviderType ProviderType { get; set; }
        public ImageProviderSettings Settings { get; set; }
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creates an empty provider entry.
        /// </summary>
        public ImageProviderEntry() { }

        /// <summary>
        /// Creates a provider entry with type and settings.
        /// </summary>
        /// <param name="providerType_">Provider type.</param>
        /// <param name="settings_">Provider settings.</param>
        public ImageProviderEntry(ImageProviderType providerType_, ImageProviderSettings settings_)
        {
            ProviderType = providerType_;
            Settings = settings_;
        }
    }
}
