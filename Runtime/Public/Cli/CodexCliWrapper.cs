namespace Weppy.AIProvider.Chat
{
    /// <summary>
    /// Helper for locating Codex CLI and its dependencies.
    /// </summary>
    public partial class CodexCliWrapper
    {
        public const string AUTO_MODEL_ID = "Auto";
        /// <summary>
        /// Finds the Codex CLI executable path on the current machine.
        /// </summary>
        /// <returns>The resolved executable path or null if not found.</returns>
        public static string FindCodexExecutablePath()
        {
            return FindCodexExecutablePathInternal();
        }

        /// <summary>
        /// Gets common Codex CLI install paths for the current platform.
        /// </summary>
        /// <returns>Candidate paths for Codex CLI.</returns>
        public static string[] GetCommonCodexPaths()
        {
            return GetCommonCodexPathsInternal();
        }

        /// <summary>
        /// Finds the Node.js executable path on the current machine.
        /// </summary>
        /// <returns>The resolved Node.js path or null if not found.</returns>
        public static string FindNodeExecutablePath()
        {
            return FindNodeExecutablePathInternal();
        }

        /// <summary>
        /// Gets common Node.js install paths for the current platform.
        /// </summary>
        /// <returns>Candidate paths for Node.js.</returns>
        public static string[] GetCommonNodePaths()
        {
            return GetCommonNodePathsInternal();
        }
    }
}
