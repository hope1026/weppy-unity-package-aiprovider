namespace Weppy.AIProvider
{
    internal static class GeminiChatCliConfig
    {
        internal const string DEFAULT_EXECUTABLE_NAME = "gemini";
        internal const string DEFAULT_CHAT_ARGUMENTS = "--output-format json";
        internal const string DEFAULT_PERSISTENT_ARGUMENTS = "-o stream-json";
        internal const string GEMINI_API_KEY_ENV = "GEMINI_API_KEY";
    }
}
