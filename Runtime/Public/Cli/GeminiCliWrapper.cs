namespace Weppy.AIProvider
{
    /// <summary>
    /// Helper for locating Gemini CLI and its dependencies.
    /// </summary>
    public partial class GeminiCliWrapper
    {
        public const string AUTO_MODEL_ID = "Auto";
        /// <summary>
        /// Finds the Gemini CLI executable path on the current machine.
        /// </summary>
        /// <returns>The resolved executable path or null if not found.</returns>
        public static string FindGeminiExecutablePath()
        {
            return FindGeminiExecutablePathInternal();
        }

        /// <summary>
        /// Gets common Gemini CLI install paths for the current platform.
        /// </summary>
        /// <returns>Candidate paths for Gemini CLI.</returns>
        public static string[] GetCommonGeminiPaths()
        {
            return GetCommonGeminiPathsInternal();
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
