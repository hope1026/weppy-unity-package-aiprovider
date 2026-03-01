namespace Weppy.AIProvider
{
    /// <summary>
    /// Preset model IDs for chat providers.
    /// </summary>
    public static class ChatModelPresets
    {
        /// <summary>
        /// OpenAI chat model presets.
        /// </summary>
        public static class OpenAI
        {
            /// <summary>
            /// Cost-effective multimodal model.
            /// </summary>
            public const string GPT_4O_MINI = "gpt-4o-mini";
        }

        /// <summary>
        /// Google Gemini chat model presets.
        /// </summary>
        public static class Google
        {
            /// <summary>
            /// Fast multimodal with 1M context.
            /// </summary>
            public const string GEMINI_25_FLASH = "gemini-2.5-flash";
        }

        /// <summary>
        /// Anthropic Claude chat model presets.
        /// </summary>
        public static class Anthropic
        {
            /// <summary>
            /// Strong performance for complex tasks.
            /// </summary>
            public const string CLAUDE_SONNET_45 = "claude-sonnet-4-5-20250929";
        }

        /// <summary>
        /// HuggingFace chat model presets.
        /// </summary>
        public static class HuggingFace
        {
            /// <summary>
            /// Large multilingual model with high performance.
            /// </summary>
            public const string QWEN_25_72B_INSTRUCT = "Qwen/Qwen2.5-72B-Instruct";
        }

        /// <summary>
        /// OpenRouter chat model presets.
        /// Routes to multiple AI providers with unified API.
        /// </summary>
        public static class OpenRouter
        {
            /// <summary>
            /// Llama 3.3 70B via OpenRouter - Free tier available.
            /// </summary>
            public const string LLAMA_33_70B_FREE = "meta-llama/llama-3.3-70b-instruct:free";
        }
    }
}
