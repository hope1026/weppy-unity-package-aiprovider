namespace Weppy.AIProvider.Chat.Editor
{
    public static class EditorDataStorageKeys
    {
        public const string KEY_OPENAI = "openai_api_key";
        public const string KEY_CODEX = "codex_api_key";
        public const string KEY_CLAUDE_CODE = "claude_code_api_key";
        public const string KEY_GEMINI_CLI = "gemini_cli_api_key";
        public const string KEY_GOOGLE = "google_api_key";
        public const string KEY_ANTHROPIC = "anthropic_api_key";
        public const string KEY_HUGGINGFACE = "huggingface_api_key";
        public const string KEY_OPENROUTER = "openrouter_api_key";
        public const string KEY_LANGUAGE = "editor_language";
        public const string KEY_CODEX_CLI_USE_API_KEY = "codex_cli_use_api_key";
        public const string KEY_CODEX_CLI_PATH = "codex_cli_path";
        public const string KEY_CODEX_NODE_PATH = "codex_node_path";
        public const string KEY_CLAUDE_CODE_CLI_USE_API_KEY = "claude_code_cli_use_api_key";
        public const string KEY_CLAUDE_CODE_CLI_PATH = "claude_code_cli_path";
        public const string KEY_CLAUDE_CODE_NODE_PATH = "claude_code_node_path";
        public const string KEY_GEMINI_CLI_USE_API_KEY = "gemini_cli_use_api_key";
        public const string KEY_GEMINI_CLI_PATH = "gemini_cli_path";
        public const string KEY_GEMINI_CLI_NODE_PATH = "gemini_cli_node_path";

        // Chat Model Keys
        public const string KEY_CUSTOM_CHAT_MODELS_OPENAI = "custom_chat_models_openai";
        public const string KEY_CUSTOM_CHAT_MODELS_CODEX_CLI = "custom_chat_models_codex_cli";
        public const string KEY_CUSTOM_CHAT_MODELS_CLAUDE_CODE_CLI = "custom_chat_models_claude_code_cli";
        public const string KEY_CUSTOM_CHAT_MODELS_GEMINI_CLI = "custom_chat_models_gemini_cli";
        public const string KEY_CUSTOM_CHAT_MODELS_GOOGLE = "custom_chat_models_google";
        public const string KEY_CUSTOM_CHAT_MODELS_ANTHROPIC = "custom_chat_models_anthropic";
        public const string KEY_CUSTOM_CHAT_MODELS_HUGGINGFACE = "custom_chat_models_huggingface";
        public const string KEY_CUSTOM_CHAT_MODELS_OPENROUTER = "custom_chat_models_openrouter";

        public const string KEY_SELECTED_CHAT_MODEL_OPENAI = "selected_chat_model_openai";
        public const string KEY_SELECTED_CHAT_MODEL_CODEX_CLI = "selected_chat_model_codex_cli";
        public const string KEY_SELECTED_CHAT_MODEL_CLAUDE_CODE_CLI = "selected_chat_model_claude_code_cli";
        public const string KEY_SELECTED_CHAT_MODEL_GEMINI_CLI = "selected_chat_model_gemini_cli";
        public const string KEY_SELECTED_CHAT_MODEL_GOOGLE = "selected_chat_model_google";
        public const string KEY_SELECTED_CHAT_MODEL_ANTHROPIC = "selected_chat_model_anthropic";
        public const string KEY_SELECTED_CHAT_MODEL_HUGGINGFACE = "selected_chat_model_huggingface";
        public const string KEY_SELECTED_CHAT_MODEL_OPENROUTER = "selected_chat_model_openrouter";
        
        public const string KEY_CUSTOM_PRESETS_FILE_PATH = "custom_presets_file_path";

        // UI State Keys
        public const string KEY_MAIN_TAB = "ui_main_tab";
        public const string KEY_LAST_CHAT_ATTACHMENT_PATH = "ui_last_chat_attachment_path";

        public static string GetApiKey(EditorDataStorage storage_, ChatProviderType providerType_)
        {
            if (storage_ == null)
                return "";

            string key = providerType_ switch
            {
                ChatProviderType.OPEN_AI => KEY_OPENAI,
                ChatProviderType.GOOGLE => KEY_GOOGLE,
                ChatProviderType.ANTHROPIC => KEY_ANTHROPIC,
                ChatProviderType.HUGGING_FACE => KEY_HUGGINGFACE,
                ChatProviderType.OPEN_ROUTER => KEY_OPENROUTER,
                _ => ""
            };

            if (string.IsNullOrEmpty(key))
                return "";

            return storage_.GetString(key);
        }

        public static bool HasAnyChatProviderAuth(EditorDataStorage storage_)
        {
            if (storage_ == null)
                return false;

            bool useCodexCliApiKey = storage_.GetBool(KEY_CODEX_CLI_USE_API_KEY, false);
            bool hasCodexCliAuth = !useCodexCliApiKey && !string.IsNullOrEmpty(storage_.GetString(KEY_CODEX_CLI_PATH));
            bool hasCodexCliApiKey = useCodexCliApiKey && !string.IsNullOrEmpty(storage_.GetString(KEY_CODEX));

            bool useClaudeCodeCliApiKey = storage_.GetBool(KEY_CLAUDE_CODE_CLI_USE_API_KEY, false);
            bool hasClaudeCodeCliAuth = !useClaudeCodeCliApiKey && !string.IsNullOrEmpty(storage_.GetString(KEY_CLAUDE_CODE_CLI_PATH));
            bool hasClaudeCodeCliApiKey = useClaudeCodeCliApiKey && !string.IsNullOrEmpty(storage_.GetString(KEY_CLAUDE_CODE));

            bool useGeminiCliApiKey = storage_.GetBool(KEY_GEMINI_CLI_USE_API_KEY, false);
            bool hasGeminiCliAuth = !useGeminiCliApiKey && !string.IsNullOrEmpty(storage_.GetString(KEY_GEMINI_CLI_PATH));
            bool hasGeminiCliApiKey = useGeminiCliApiKey && !string.IsNullOrEmpty(storage_.GetString(KEY_GEMINI_CLI));

            return !string.IsNullOrEmpty(storage_.GetString(KEY_OPENAI)) ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_GOOGLE)) ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_ANTHROPIC)) ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_HUGGINGFACE)) ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_OPENROUTER)) ||
                   hasCodexCliAuth ||
                   hasCodexCliApiKey ||
                   hasClaudeCodeCliAuth ||
                   hasClaudeCodeCliApiKey ||
                   hasGeminiCliAuth ||
                   hasGeminiCliApiKey;
        }

        public static string GetCustomChatModelsKey(ChatProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatProviderType.OPEN_AI => KEY_CUSTOM_CHAT_MODELS_OPENAI,
                ChatProviderType.GOOGLE => KEY_CUSTOM_CHAT_MODELS_GOOGLE,
                ChatProviderType.ANTHROPIC => KEY_CUSTOM_CHAT_MODELS_ANTHROPIC,
                ChatProviderType.HUGGING_FACE => KEY_CUSTOM_CHAT_MODELS_HUGGINGFACE,
                ChatProviderType.OPEN_ROUTER => KEY_CUSTOM_CHAT_MODELS_OPENROUTER,
                _ => ""
            };
        }

        public static string GetSelectedChatModelKey(ChatProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatProviderType.OPEN_AI => KEY_SELECTED_CHAT_MODEL_OPENAI,
                ChatProviderType.GOOGLE => KEY_SELECTED_CHAT_MODEL_GOOGLE,
                ChatProviderType.ANTHROPIC => KEY_SELECTED_CHAT_MODEL_ANTHROPIC,
                ChatProviderType.HUGGING_FACE => KEY_SELECTED_CHAT_MODEL_HUGGINGFACE,
                ChatProviderType.OPEN_ROUTER => KEY_SELECTED_CHAT_MODEL_OPENROUTER,
                _ => ""
            };
        }

        public static string GetSelectedChatModelListKey(ChatProviderType providerType_)
        {
            string baseKey = GetSelectedChatModelKey(providerType_);
            if (string.IsNullOrEmpty(baseKey))
                return "";
            return baseKey + "_list";
        }

        public static string GetApiKey(EditorDataStorage storage_, ChatEditorProviderType providerType_)
        {
            if (storage_ == null)
                return "";

            return providerType_ switch
            {
                ChatEditorProviderType.OPEN_AI => storage_.GetString(KEY_OPENAI),
                ChatEditorProviderType.GOOGLE => storage_.GetString(KEY_GOOGLE),
                ChatEditorProviderType.ANTHROPIC => storage_.GetString(KEY_ANTHROPIC),
                ChatEditorProviderType.HUGGING_FACE => storage_.GetString(KEY_HUGGINGFACE),
                ChatEditorProviderType.OPEN_ROUTER => storage_.GetString(KEY_OPENROUTER),
                ChatEditorProviderType.CODEX_CLI => storage_.GetString(KEY_CODEX),
                ChatEditorProviderType.CLAUDE_CODE_CLI => storage_.GetString(KEY_CLAUDE_CODE),
                ChatEditorProviderType.GEMINI_CLI => storage_.GetString(KEY_GEMINI_CLI),
                _ => ""
            };
        }

        public static string GetCustomChatModelsKey(ChatEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatEditorProviderType.OPEN_AI => KEY_CUSTOM_CHAT_MODELS_OPENAI,
                ChatEditorProviderType.CODEX_CLI => KEY_CUSTOM_CHAT_MODELS_CODEX_CLI,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CUSTOM_CHAT_MODELS_CLAUDE_CODE_CLI,
                ChatEditorProviderType.GEMINI_CLI => KEY_CUSTOM_CHAT_MODELS_GEMINI_CLI,
                ChatEditorProviderType.GOOGLE => KEY_CUSTOM_CHAT_MODELS_GOOGLE,
                ChatEditorProviderType.ANTHROPIC => KEY_CUSTOM_CHAT_MODELS_ANTHROPIC,
                ChatEditorProviderType.HUGGING_FACE => KEY_CUSTOM_CHAT_MODELS_HUGGINGFACE,
                ChatEditorProviderType.OPEN_ROUTER => KEY_CUSTOM_CHAT_MODELS_OPENROUTER,
                _ => ""
            };
        }

        public static string GetSelectedChatModelKey(ChatEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatEditorProviderType.OPEN_AI => KEY_SELECTED_CHAT_MODEL_OPENAI,
                ChatEditorProviderType.CODEX_CLI => KEY_SELECTED_CHAT_MODEL_CODEX_CLI,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_SELECTED_CHAT_MODEL_CLAUDE_CODE_CLI,
                ChatEditorProviderType.GEMINI_CLI => KEY_SELECTED_CHAT_MODEL_GEMINI_CLI,
                ChatEditorProviderType.GOOGLE => KEY_SELECTED_CHAT_MODEL_GOOGLE,
                ChatEditorProviderType.ANTHROPIC => KEY_SELECTED_CHAT_MODEL_ANTHROPIC,
                ChatEditorProviderType.HUGGING_FACE => KEY_SELECTED_CHAT_MODEL_HUGGINGFACE,
                ChatEditorProviderType.OPEN_ROUTER => KEY_SELECTED_CHAT_MODEL_OPENROUTER,
                _ => ""
            };
        }

        public static string GetSelectedChatModelListKey(ChatEditorProviderType providerType_)
        {
            string baseKey = GetSelectedChatModelKey(providerType_);
            if (string.IsNullOrEmpty(baseKey))
                return "";
            return baseKey + "_list";
        }

        public static bool GetCliUseApiKey(EditorDataStorage storage_, ChatEditorProviderType providerType_)
        {
            if (storage_ == null)
                return false;

            string key = providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => KEY_CODEX_CLI_USE_API_KEY,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CLAUDE_CODE_CLI_USE_API_KEY,
                ChatEditorProviderType.GEMINI_CLI => KEY_GEMINI_CLI_USE_API_KEY,
                _ => null
            };

            return !string.IsNullOrEmpty(key) && storage_.GetBool(key, false);
        }

        public static void SetCliUseApiKey(EditorDataStorage storage_, ChatEditorProviderType providerType_, bool useApiKey_)
        {
            if (storage_ == null)
                return;

            string key = providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => KEY_CODEX_CLI_USE_API_KEY,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CLAUDE_CODE_CLI_USE_API_KEY,
                ChatEditorProviderType.GEMINI_CLI => KEY_GEMINI_CLI_USE_API_KEY,
                _ => null
            };

            if (string.IsNullOrEmpty(key))
                return;

            storage_.SetBool(key, useApiKey_);
            storage_.Save();
        }

        public static string GetCliApiKey(EditorDataStorage storage_, ChatEditorProviderType providerType_)
        {
            if (storage_ == null)
                return string.Empty;

            string key = providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => KEY_CODEX,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CLAUDE_CODE,
                ChatEditorProviderType.GEMINI_CLI => KEY_GEMINI_CLI,
                _ => null
            };

            return string.IsNullOrEmpty(key) ? string.Empty : storage_.GetString(key);
        }

        public static string GetCliExecutablePath(EditorDataStorage storage_, ChatEditorProviderType providerType_)
        {
            if (storage_ == null)
                return string.Empty;

            string key = providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => KEY_CODEX_CLI_PATH,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CLAUDE_CODE_CLI_PATH,
                ChatEditorProviderType.GEMINI_CLI => KEY_GEMINI_CLI_PATH,
                _ => null
            };

            return string.IsNullOrEmpty(key) ? string.Empty : storage_.GetString(key);
        }

        public static void SetCliExecutablePath(EditorDataStorage storage_, ChatEditorProviderType providerType_, string path_)
        {
            if (storage_ == null)
                return;

            string key = providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => KEY_CODEX_CLI_PATH,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CLAUDE_CODE_CLI_PATH,
                ChatEditorProviderType.GEMINI_CLI => KEY_GEMINI_CLI_PATH,
                _ => null
            };

            if (string.IsNullOrEmpty(key))
                return;

            storage_.SetString(key, path_ ?? string.Empty);
            storage_.Save();
        }

        public static string GetNodeExecutablePath(EditorDataStorage storage_, ChatEditorProviderType providerType_)
        {
            if (storage_ == null)
                return string.Empty;

            string key = providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => KEY_CODEX_NODE_PATH,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CLAUDE_CODE_NODE_PATH,
                ChatEditorProviderType.GEMINI_CLI => KEY_GEMINI_CLI_NODE_PATH,
                _ => null
            };

            return string.IsNullOrEmpty(key) ? string.Empty : storage_.GetString(key);
        }

        public static void SetNodeExecutablePath(EditorDataStorage storage_, ChatEditorProviderType providerType_, string path_)
        {
            if (storage_ == null)
                return;

            string key = providerType_ switch
            {
                ChatEditorProviderType.CODEX_CLI => KEY_CODEX_NODE_PATH,
                ChatEditorProviderType.CLAUDE_CODE_CLI => KEY_CLAUDE_CODE_NODE_PATH,
                ChatEditorProviderType.GEMINI_CLI => KEY_GEMINI_CLI_NODE_PATH,
                _ => null
            };

            if (string.IsNullOrEmpty(key))
                return;

            storage_.SetString(key, path_ ?? string.Empty);
            storage_.Save();
        }

        public static string GetModelPriorityKey(string providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(providerType_) || string.IsNullOrEmpty(modelId_))
                return "";

            string safeProvider = SanitizeKeySegment(providerType_);
            string safeModel = SanitizeKeySegment(modelId_);
            return $"model_priority_{safeProvider}_{safeModel}";
        }

        private static string SanitizeKeySegment(string value_)
        {
            return value_.Replace("/", "_").Replace(" ", "_");
        }
    }
}