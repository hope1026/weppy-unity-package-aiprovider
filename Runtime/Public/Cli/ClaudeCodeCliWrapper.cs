namespace Weppy.AIProvider
{
    /// <summary>
    /// Helper for locating Claude Code CLI and its dependencies.
    /// </summary>
    public partial class ClaudeCodeCliWrapper
    {
        public const string AUTO_MODEL_ID = "Auto";
        /// <summary>
        /// Finds the Claude Code CLI executable path on the current machine.
        /// </summary>
        /// <returns>The resolved executable path or null if not found.</returns>
        public static string FindClaudeCodeExecutablePath()
        {
            return FindClaudeCodeExecutablePathInternal();
        }

        /// <summary>
        /// Gets common Claude Code CLI install paths for the current platform.
        /// </summary>
        /// <returns>Candidate paths for Claude Code CLI.</returns>
        public static string[] GetCommonClaudeCodePaths()
        {
            return GetCommonClaudeCodePathsInternal();
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
