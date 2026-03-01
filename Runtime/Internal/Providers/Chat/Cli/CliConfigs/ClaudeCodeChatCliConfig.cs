namespace Weppy.AIProvider
{
    internal static class ClaudeCodeChatCliConfig
    {
        internal const string DEFAULT_EXECUTABLE_NAME = "claude";
        internal const string DEFAULT_CHAT_ARGUMENTS = "-p --output-format json";
        internal const string DEFAULT_PERSISTENT_ARGUMENTS = "-p --input-format stream-json --output-format stream-json --verbose";
        internal const string ANTHROPIC_API_KEY_ENV = "ANTHROPIC_API_KEY";
    }
}
