namespace Weppy.AIProvider
{
    /// <summary>
    /// Helper for locating Codex App executable paths used for app-server mode.
    /// </summary>
    public partial class CodexAppWrapper
    {
        /// <summary>
        /// Finds the Codex App executable path on the current machine.
        /// </summary>
        /// <returns>The resolved executable path or null if not found.</returns>
        public static string FindCodexAppExecutablePath()
        {
            return FindCodexAppExecutablePathInternal();
        }

        /// <summary>
        /// Gets common Codex App install paths for the current platform.
        /// </summary>
        /// <returns>Candidate paths for Codex App executable.</returns>
        public static string[] GetCommonCodexAppPaths()
        {
            return GetCommonCodexAppPathsInternal();
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
