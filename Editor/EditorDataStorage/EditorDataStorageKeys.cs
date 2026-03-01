namespace Weppy.AIProvider.Editor
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
        public const string KEY_REMOVEBG = "removebg_api_key";
        public const string KEY_LANGUAGE = "editor_language";
        public const string KEY_CODEX_CLI_USE_API_KEY = "codex_cli_use_api_key";
        public const string KEY_CODEX_CLI_PATH = "codex_cli_path";
        public const string KEY_CODEX_NODE_PATH = "codex_node_path";
        public const string KEY_CODEX_APP_USE_API_KEY = "codex_app_use_api_key";
        public const string KEY_CODEX_APP_PATH = "codex_app_path";
        public const string KEY_CODEX_APP_NODE_PATH = "codex_app_node_path";
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

        public const string KEY_ENABLED_CHAT_MODELS = "enabled_chat_models";
        public const string KEY_ENABLED_IMAGE_MODELS = "enabled_image_models";
        public const string KEY_ENABLED_BGREMOVAL_MODELS = "enabled_bgremoval_models";
        public const string KEY_CUSTOM_PRESETS_FILE_PATH = "custom_presets_file_path";

        public const string KEY_CUSTOM_IMAGE_MODELS_OPENAI = "custom_image_models_openai";
        public const string KEY_CUSTOM_IMAGE_MODELS_CODEX_CLI = "custom_image_models_codex_cli";
        public const string KEY_CUSTOM_IMAGE_MODELS_GOOGLE_GEMINI = "custom_image_models_google_gemini";
        public const string KEY_CUSTOM_IMAGE_MODELS_GOOGLE_IMAGEN = "custom_image_models_google_imagen";
        public const string KEY_CUSTOM_IMAGE_MODELS_HUGGINGFACE = "custom_image_models_huggingface";
        public const string KEY_CUSTOM_IMAGE_MODELS_OPENROUTER = "custom_image_models_openrouter";

        public const string KEY_SELECTED_IMAGE_MODEL_OPENAI = "selected_image_model_openai";
        public const string KEY_SELECTED_IMAGE_MODEL_CODEX_CLI = "selected_image_model_codex_cli";
        public const string KEY_SELECTED_IMAGE_MODEL_GOOGLE_GEMINI = "selected_image_model_google_gemini";
        public const string KEY_SELECTED_IMAGE_MODEL_GOOGLE_IMAGEN = "selected_image_model_google_imagen";
        public const string KEY_SELECTED_IMAGE_MODEL_HUGGINGFACE = "selected_image_model_huggingface";
        public const string KEY_SELECTED_IMAGE_MODEL_OPENROUTER = "selected_image_model_openrouter";

        public const string KEY_SELECTED_BGREMOVAL_MODEL_REMOVEBG = "selected_bgremoval_model_removebg";

        // UI State Keys
        public const string KEY_MAIN_TAB = "ui_main_tab";
        public const string KEY_IMAGE_DYNAMIC_OPTIONS_COLLAPSED = "ui_image_dynamic_options_collapsed";
        public const string KEY_LAST_CHAT_ATTACHMENT_PATH = "ui_last_chat_attachment_path";
        public const string KEY_LAST_IMAGE_INPUT_PATH = "ui_last_image_input_path";
        public const string KEY_LAST_BGREMOVAL_INPUT_PATH = "ui_last_bgremoval_input_path";

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

        public static string GetApiKey(EditorDataStorage storage_, ImageProviderType generationProviderType)
        {
            if (storage_ == null)
                return "";

            return generationProviderType switch
            {
                ImageProviderType.OPEN_AI => storage_.GetString(KEY_OPENAI),
                ImageProviderType.GOOGLE_GEMINI => storage_.GetString(KEY_GOOGLE),
                ImageProviderType.GOOGLE_IMAGEN => storage_.GetString(KEY_GOOGLE),
                ImageProviderType.OPEN_ROUTER => storage_.GetString(KEY_OPENROUTER),
                _ => ""
            };
        }

        public static bool HasAnyImageProviderAuth(EditorDataStorage storage_)
        {
            if (storage_ == null)
                return false;

            bool useAppApiKey = GetImageAppUseApiKey(storage_, ImageEditorProviderType.CODEX_APP);
            string appExecutablePath = GetImageAppExecutablePath(storage_, ImageEditorProviderType.CODEX_APP);
            bool hasAppAuth = !useAppApiKey && IsExecutablePathValid(appExecutablePath);
            bool hasAppApiKey = useAppApiKey && !string.IsNullOrEmpty(storage_.GetString(KEY_CODEX));

            return hasAppAuth ||
                   hasAppApiKey ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_OPENAI)) ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_GOOGLE)) ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_HUGGINGFACE)) ||
                   !string.IsNullOrEmpty(storage_.GetString(KEY_OPENROUTER));
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

        public static string GetApiKey(EditorDataStorage storage_, ImageEditorProviderType providerType_)
        {
            if (storage_ == null)
                return "";

            return providerType_ switch
            {
                ImageEditorProviderType.OPEN_AI => storage_.GetString(KEY_OPENAI),
                ImageEditorProviderType.CODEX_APP => storage_.GetString(KEY_CODEX),
                ImageEditorProviderType.GOOGLE_GEMINI => storage_.GetString(KEY_GOOGLE),
                ImageEditorProviderType.GOOGLE_IMAGEN => storage_.GetString(KEY_GOOGLE),
                ImageEditorProviderType.OPEN_ROUTER => storage_.GetString(KEY_OPENROUTER),
                _ => ""
            };
        }

        public static string GetCustomImageModelsKey(ImageEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ImageEditorProviderType.OPEN_AI => KEY_CUSTOM_IMAGE_MODELS_OPENAI,
                ImageEditorProviderType.CODEX_APP => KEY_CUSTOM_IMAGE_MODELS_CODEX_CLI,
                ImageEditorProviderType.GOOGLE_GEMINI => KEY_CUSTOM_IMAGE_MODELS_GOOGLE_GEMINI,
                ImageEditorProviderType.GOOGLE_IMAGEN => KEY_CUSTOM_IMAGE_MODELS_GOOGLE_IMAGEN,
                ImageEditorProviderType.OPEN_ROUTER => KEY_CUSTOM_IMAGE_MODELS_OPENROUTER,
                _ => ""
            };
        }

        public static string GetSelectedImageModelKey(ImageEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ImageEditorProviderType.OPEN_AI => KEY_SELECTED_IMAGE_MODEL_OPENAI,
                ImageEditorProviderType.CODEX_APP => KEY_SELECTED_IMAGE_MODEL_CODEX_CLI,
                ImageEditorProviderType.GOOGLE_GEMINI => KEY_SELECTED_IMAGE_MODEL_GOOGLE_GEMINI,
                ImageEditorProviderType.GOOGLE_IMAGEN => KEY_SELECTED_IMAGE_MODEL_GOOGLE_IMAGEN,
                ImageEditorProviderType.OPEN_ROUTER => KEY_SELECTED_IMAGE_MODEL_OPENROUTER,
                _ => ""
            };
        }

        public static bool GetImageAppUseApiKey(EditorDataStorage storage_, ImageEditorProviderType providerType_)
        {
            if (storage_ == null)
                return false;

            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return false;

            if (storage_.HasKey(KEY_CODEX_APP_USE_API_KEY))
                return storage_.GetBool(KEY_CODEX_APP_USE_API_KEY, false);

            bool legacyValue = storage_.GetBool(KEY_CODEX_CLI_USE_API_KEY, false);
            if (storage_.HasKey(KEY_CODEX_CLI_USE_API_KEY))
            {
                storage_.SetBool(KEY_CODEX_APP_USE_API_KEY, legacyValue);
                storage_.Save();
            }

            return legacyValue;
        }

        public static void SetImageAppUseApiKey(EditorDataStorage storage_, ImageEditorProviderType providerType_, bool useApiKey_)
        {
            if (storage_ == null)
                return;

            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return;

            storage_.SetBool(KEY_CODEX_APP_USE_API_KEY, useApiKey_);
            storage_.Save();
        }

        public static string GetImageAppApiKey(EditorDataStorage storage_, ImageEditorProviderType providerType_)
        {
            if (storage_ == null)
                return string.Empty;

            return providerType_ switch
            {
                ImageEditorProviderType.CODEX_APP => storage_.GetString(KEY_CODEX),
                _ => string.Empty
            };
        }

        public static string GetImageAppExecutablePath(EditorDataStorage storage_, ImageEditorProviderType providerType_)
        {
            if (storage_ == null)
                return string.Empty;

            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return string.Empty;

            string appPath = NormalizeCodexAppExecutablePath(storage_.GetString(KEY_CODEX_APP_PATH));
            if (!string.IsNullOrEmpty(appPath))
            {
                if (storage_.GetString(KEY_CODEX_APP_PATH) != appPath)
                {
                    storage_.SetString(KEY_CODEX_APP_PATH, appPath);
                    storage_.Save();
                }
                return appPath;
            }

            // Backward compatibility with previous image app path storage.
            string legacyPath = NormalizeCodexAppExecutablePath(storage_.GetString(KEY_CODEX_CLI_PATH));
            if (!string.IsNullOrEmpty(legacyPath))
            {
                storage_.SetString(KEY_CODEX_APP_PATH, legacyPath);
                storage_.Save();
            }

            return legacyPath;
        }

        public static void SetImageAppExecutablePath(EditorDataStorage storage_, ImageEditorProviderType providerType_, string path_)
        {
            if (storage_ == null)
                return;

            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return;

            storage_.SetString(KEY_CODEX_APP_PATH, NormalizeCodexAppExecutablePath(path_) ?? string.Empty);
            storage_.Save();
        }

        private static string NormalizeCodexAppExecutablePath(string path_)
        {
            if (string.IsNullOrWhiteSpace(path_))
                return string.Empty;

            string trimmed = path_.Trim().TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(trimmed))
                return string.Empty;

            string normalizedSeparators = trimmed.Replace('\\', '/');

            // Accept any path inside a macOS .app bundle and resolve to the executable used by app-server.
            int appSuffixIndex = normalizedSeparators.IndexOf(".app", System.StringComparison.OrdinalIgnoreCase);
            if (appSuffixIndex >= 0)
            {
                string appRoot = normalizedSeparators.Substring(0, appSuffixIndex + 4);
                string resolvedFromAppBundle = ResolveCodexAppExecutablePath(appRoot);
                if (!string.IsNullOrEmpty(resolvedFromAppBundle))
                    return resolvedFromAppBundle;
            }

            // If user selected Resources/MacOS directories directly, resolve from there as well.
            if (System.IO.Directory.Exists(trimmed))
            {
                string resolvedFromDirectory = ResolveCodexAppExecutablePath(trimmed);
                if (!string.IsNullOrEmpty(resolvedFromDirectory))
                    return resolvedFromDirectory;
            }

            return trimmed;
        }

        private static string ResolveCodexAppExecutablePath(string basePath_)
        {
            if (string.IsNullOrWhiteSpace(basePath_))
                return string.Empty;

            string basePath = basePath_.Trim().TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(basePath))
                return string.Empty;

            string[] candidates =
            {
                basePath,
                CombinePath(basePath, "codex"),
                CombinePath(basePath, "Codex"),
                CombinePath(basePath, "Contents", "Resources", "codex"),
                CombinePath(basePath, "Contents", "MacOS", "Codex"),
                CombinePath(basePath, "Resources", "codex"),
                CombinePath(basePath, "MacOS", "Codex")
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate))
                    continue;

                if (System.IO.File.Exists(candidate))
                    return candidate;
            }

            return string.Empty;
        }

        private static string CombinePath(string basePath_, params string[] segments_)
        {
            if (string.IsNullOrWhiteSpace(basePath_))
                return string.Empty;

            string combined = basePath_;
            foreach (string segment in segments_)
            {
                if (string.IsNullOrEmpty(segment))
                    continue;

                combined = System.IO.Path.Combine(combined, segment);
            }

            return combined;
        }

        private static bool IsExecutablePathValid(string path_)
        {
            if (string.IsNullOrWhiteSpace(path_))
                return false;

            string trimmedPath = path_.Trim();
            if (!System.IO.Path.IsPathRooted(trimmedPath))
                return false;

            return System.IO.File.Exists(trimmedPath);
        }

        public static string GetImageAppNodeExecutablePath(EditorDataStorage storage_, ImageEditorProviderType providerType_)
        {
            if (storage_ == null)
                return string.Empty;

            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return string.Empty;

            string nodePath = storage_.GetString(KEY_CODEX_APP_NODE_PATH);
            if (!string.IsNullOrEmpty(nodePath))
                return nodePath;

            // Backward compatibility with previous shared codex node path storage.
            string legacyPath = storage_.GetString(KEY_CODEX_NODE_PATH);
            if (!string.IsNullOrEmpty(legacyPath))
            {
                storage_.SetString(KEY_CODEX_APP_NODE_PATH, legacyPath);
                storage_.Save();
            }

            return legacyPath;
        }

        public static void SetImageAppNodeExecutablePath(EditorDataStorage storage_, ImageEditorProviderType providerType_, string path_)
        {
            if (storage_ == null)
                return;

            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return;

            storage_.SetString(KEY_CODEX_APP_NODE_PATH, path_ ?? string.Empty);
            storage_.Save();
        }

        public static string GetSelectedImageModelListKey(ImageEditorProviderType providerType_)
        {
            string baseKey = GetSelectedImageModelKey(providerType_);
            if (string.IsNullOrEmpty(baseKey))
                return "";
            return baseKey + "_list";
        }

        public static string GetCustomImageModelsKey(ImageProviderType generationProviderType)
        {
            return generationProviderType switch
            {
                ImageProviderType.OPEN_AI => KEY_CUSTOM_IMAGE_MODELS_OPENAI,
                ImageProviderType.GOOGLE_GEMINI => KEY_CUSTOM_IMAGE_MODELS_GOOGLE_GEMINI,
                ImageProviderType.GOOGLE_IMAGEN => KEY_CUSTOM_IMAGE_MODELS_GOOGLE_IMAGEN,
                ImageProviderType.OPEN_ROUTER => KEY_CUSTOM_IMAGE_MODELS_OPENROUTER,
                _ => ""
            };
        }

        public static string GetSelectedImageModelKey(ImageProviderType generationProviderType)
        {
            return generationProviderType switch
            {
                ImageProviderType.OPEN_AI => KEY_SELECTED_IMAGE_MODEL_OPENAI,
                ImageProviderType.GOOGLE_GEMINI => KEY_SELECTED_IMAGE_MODEL_GOOGLE_GEMINI,
                ImageProviderType.GOOGLE_IMAGEN => KEY_SELECTED_IMAGE_MODEL_GOOGLE_IMAGEN,
                ImageProviderType.OPEN_ROUTER => KEY_SELECTED_IMAGE_MODEL_OPENROUTER,
                _ => ""
            };
        }

        public static string GetSelectedImageModelListKey(ImageProviderType generationProviderType)
        {
            string baseKey = GetSelectedImageModelKey(generationProviderType);
            if (string.IsNullOrEmpty(baseKey))
                return "";
            return baseKey + "_list";
        }

        public static string GetApiKey(EditorDataStorage storage_, BgRemovalProviderType providerType_)
        {
            if (storage_ == null)
                return "";

            return providerType_ switch
            {
                BgRemovalProviderType.REMOVE_BG => storage_.GetString(KEY_REMOVEBG),
                _ => ""
            };
        }

        public static bool HasAnyBgRemovalProviderKey(EditorDataStorage storage_)
        {
            if (storage_ == null)
                return false;

            return !string.IsNullOrEmpty(storage_.GetString(KEY_REMOVEBG));
        }

        public static string GetSelectedBgRemovalModelKey(BgRemovalProviderType providerType_)
        {
            return providerType_ switch
            {
                BgRemovalProviderType.REMOVE_BG => KEY_SELECTED_BGREMOVAL_MODEL_REMOVEBG,
                _ => ""
            };
        }

        public static string GetSelectedBgRemovalModelListKey(BgRemovalProviderType providerType_)
        {
            string baseKey = GetSelectedBgRemovalModelKey(providerType_);
            if (string.IsNullOrEmpty(baseKey))
                return "";
            return baseKey + "_list";
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
