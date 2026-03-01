namespace Weppy.AIProvider
{
    /// <summary>
    /// Preset size options for background removal providers.
    /// </summary>
    public static class BgRemovalModelPresets
    {
        /// <summary>
        /// Remove.bg background removal size presets.
        /// </summary>
        public static class RemoveBg
        {
            /// <summary>
            /// Automatic size selection based on input image.
            /// Recommended for most use cases.
            /// </summary>
            public const string AUTO = "auto";
        }
    }
}
