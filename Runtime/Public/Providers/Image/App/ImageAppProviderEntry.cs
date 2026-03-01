namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a configured app-based image provider entry.
    /// </summary>
    public class ImageAppProviderEntry
    {
        public ImageAppProviderType ProviderType { get; set; }
        public ImageAppProviderSettings Settings { get; set; }
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Creates an empty provider entry.
        /// </summary>
        public ImageAppProviderEntry()
        {
        }

        /// <summary>
        /// Creates a provider entry with type and settings.
        /// </summary>
        /// <param name="providerType_">Provider type.</param>
        /// <param name="settings_">Provider settings.</param>
        public ImageAppProviderEntry(ImageAppProviderType providerType_, ImageAppProviderSettings settings_)
        {
            ProviderType = providerType_;
            Settings = settings_;
        }
    }
}
