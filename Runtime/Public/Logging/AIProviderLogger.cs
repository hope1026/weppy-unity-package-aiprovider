namespace Weppy.AIProvider
{
    /// <summary>
    /// Controls logging verbosity for the AI Provider package.
    /// </summary>
    public static partial class AIProviderLogger
    {
        /// <summary>
        /// Sets the global log level for AI Provider logging.
        /// </summary>
        /// <param name="level_">The log level to apply.</param>
        public static void SetLogLevel(LogLevel level_)
        {
            SetLogLevelInternal(level_);
        }
    }
}
