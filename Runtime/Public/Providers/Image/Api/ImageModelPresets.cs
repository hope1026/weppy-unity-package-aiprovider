namespace Weppy.AIProvider
{
    /// <summary>
    /// Preset model IDs for image generation providers.
    /// </summary>
    public static class ImageModelPresets
    {
        /// <summary>
        /// OpenAI image generation model presets.
        /// </summary>
        public static class OpenAI
        {
            /// <summary>
            /// Latest flagship image model.
            /// Sizes: 1024x1024, 1024x1536, 1536x1024
            /// </summary>
            public const string GPT_IMAGE_1 = "gpt-image-1";
        }

        /// <summary>
        /// Codex App image generation presets.
        /// </summary>
        public static class CodexApp
        {
            /// <summary>
            /// Default model id for Codex App image generation.
            /// </summary>
            public const string CODEX_APP_IMAGE = "codex-app-image";
        }

        /// <summary>
        /// Google Gemini image generation model presets.
        /// </summary>
        public static class GoogleGemini
        {
            /// <summary>
            /// Fast image generation.
            /// Supports up to 2K resolution.
            /// </summary>
            public const string GEMINI_25_FLASH_IMAGE = "gemini-2.5-flash-image";
        }

        /// <summary>
        /// Google Imagen image generation model presets.
        /// </summary>
        public static class GoogleImagen
        {
            /// <summary>
            /// High-quality versatile image generation.
            /// </summary>
            public const string IMAGEN_4 = "imagen-4.0-generate-001";
        }

        /// <summary>
        /// HuggingFace image generation model presets.
        /// </summary>
        public static class HuggingFace
        {
            /// <summary>
            /// Fast text-to-image generation.
            /// Optimized for speed.
            /// </summary>
            public const string FLUX_1_SCHNELL = "black-forest-labs/FLUX.1-schnell";
        }

        /// <summary>
        /// OpenRouter image generation model presets.
        /// Routes to multiple AI providers with unified API.
        /// </summary>
        public static class OpenRouter
        {
            /// <summary>
            /// Gemini 2.5 Flash Image via OpenRouter - Fast and affordable.
            /// </summary>
            public const string GEMINI_25_FLASH_IMAGE = "google/gemini-2.5-flash-image";
        }
    }
}
