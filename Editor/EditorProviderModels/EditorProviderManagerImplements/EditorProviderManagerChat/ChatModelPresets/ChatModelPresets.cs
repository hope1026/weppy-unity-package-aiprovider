using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Weppy.AIProvider.Chat.Editor
{
    public class ChatModelPresets
    {
        public const string DEFAULT_PRESETS_RESOURCE_PATH = "DefaultChatModelPresets";

        public const string OPENAI_MODELS_URL = "https://platform.openai.com/docs/models";
        public const string CODEX_CLI_MODELS_URL = "https://developers.openai.com/codex/models";
        public const string CLAUDE_CODE_CLI_MODELS_URL = "https://docs.anthropic.com/en/docs/about-claude/models/all-models";
        public const string GEMINI_CLI_MODELS_URL = "https://geminicli.com/docs/cli/model/";
        public const string GOOGLE_MODELS_URL = "https://ai.google.dev/gemini-api/docs/models/gemini";
        public const string ANTHROPIC_MODELS_URL = "https://platform.claude.com/docs/en/about-claude/models/overview";
        public const string HUGGINGFACE_MODELS_URL = "https://huggingface.co/models?pipeline_tag=text-generation";

        private readonly Dictionary<ChatEditorProviderType, List<ChatModelInfo>> _defaultChatModels =
            new Dictionary<ChatEditorProviderType, List<ChatModelInfo>>();

        private readonly Dictionary<ChatEditorProviderType, List<ChatModelInfo>> _customChatModels =
            new Dictionary<ChatEditorProviderType, List<ChatModelInfo>>();

        private bool _defaultPresetsLoaded = false;
        private string _customPresetsFilePath = "";

        public string CustomPresetsFilePath { get => _customPresetsFilePath; set => _customPresetsFilePath = value; }

        public void LoadDefaultPresets()
        {
            if (_defaultPresetsLoaded)
                return;

            _defaultChatModels.Clear();

            string assetPath = EditorPaths.RESOURCES_PATH + DEFAULT_PRESETS_RESOURCE_PATH + ".json";
            TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (jsonAsset != null)
            {
                try
                {
                    DefaultChatPresetsJson presetsJson = JsonUtility.FromJson<DefaultChatPresetsJson>(jsonAsset.text);
                    if (presetsJson != null)
                    {
                        LoadChatProvidersFromJson(presetsJson.chatProviders, _defaultChatModels, false);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ChatModelPresets] Failed to parse default presets JSON: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[ChatModelPresets] Default presets JSON not found in Resources");
            }

            _defaultPresetsLoaded = true;
        }

        public void LoadCustomPresets(string filePath_)
        {
            _customPresetsFilePath = filePath_;
            _customChatModels.Clear();

            if (string.IsNullOrEmpty(filePath_) || !File.Exists(filePath_))
                return;

            try
            {
                string json = File.ReadAllText(filePath_);
                CustomPresetsJson customJson = JsonUtility.FromJson<CustomPresetsJson>(json);
                if (customJson != null)
                {
                    if (customJson.customChatProviders != null)
                    {
                        LoadChatProvidersFromJson(customJson.customChatProviders, _customChatModels, true);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatModelPresets] Failed to load custom presets from {filePath_}: {e.Message}");
            }

            LoadDefaultPresets();
        }

        public void SaveCustomPresets()
        {
            if (string.IsNullOrEmpty(_customPresetsFilePath))
                return;

            try
            {
                CustomPresetsJson customJson = new CustomPresetsJson();
                foreach (KeyValuePair<ChatEditorProviderType, List<ChatModelInfo>> kvp in _customChatModels)
                {
                    if (kvp.Value.Count > 0)
                    {
                        ChatModelsJson chatModelsJson = new ChatModelsJson
                        {
                            provider = kvp.Key.ToString()
                        };
                        foreach (ChatModelInfo model in kvp.Value)
                        {
                            chatModelsJson.models.Add(new ChatModelJsonEntry(model));
                        }
                        customJson.customChatProviders.Add(chatModelsJson);
                    }
                }

                string jsonText = JsonUtility.ToJson(customJson, true);
                string directory = Path.GetDirectoryName(_customPresetsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(_customPresetsFilePath, jsonText);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatModelPresets] Failed to save custom presets: {e.Message}");
            }
        }

        private static void LoadChatProvidersFromJson(
            List<ChatModelsJson> providers_,
            Dictionary<ChatEditorProviderType, List<ChatModelInfo>> target_,
            bool isCustom_)
        {
            if (providers_ == null)
                return;

            
            foreach (ChatModelsJson providerJson in providers_)
            {
                if (Enum.TryParse(providerJson.provider, out ChatEditorProviderType providerType))
                {
                    if (!target_.ContainsKey(providerType))
                    {
                        target_[providerType] = new List<ChatModelInfo>();
                    }

                    foreach (ChatModelJsonEntry modelEntry in providerJson.models)
                    {
                        ChatModelInfo modelInfo = modelEntry.ToProviderModelInfo(isCustom_);
                        target_[providerType].Add(modelInfo);
                    }
                }
            }
        }

        public string GetModelsUrl(ChatEditorProviderType providerType_)
        {
            return providerType_ switch
            {
                ChatEditorProviderType.OPEN_AI => OPENAI_MODELS_URL,
                ChatEditorProviderType.CODEX_CLI => CODEX_CLI_MODELS_URL,
                ChatEditorProviderType.CLAUDE_CODE_CLI => CLAUDE_CODE_CLI_MODELS_URL,
                ChatEditorProviderType.GEMINI_CLI => GEMINI_CLI_MODELS_URL,
                ChatEditorProviderType.GOOGLE => GOOGLE_MODELS_URL,
                ChatEditorProviderType.ANTHROPIC => ANTHROPIC_MODELS_URL,
                ChatEditorProviderType.HUGGING_FACE => HUGGINGFACE_MODELS_URL,
                _ => ""
            };
        }

        public string GetDefaultChatModel(ChatEditorProviderType providerType_)
        {
            LoadDefaultPresets();
            List<ChatModelInfo> models = GetDefaultModels(providerType_);
            if (models.Count > 0)
            {
                foreach (ChatModelInfo model in models)
                {
                    return model.Id;
                }
            }
            return "";
        }

        public List<ChatModelInfo> GetChatModelInfos(ChatEditorProviderType providerType_)
        {
            LoadDefaultPresets();
            List<ChatModelInfo> result = new List<ChatModelInfo>();

            List<ChatModelInfo> defaultModels = GetDefaultModels(providerType_);
            if (defaultModels.Count > 0)
            {
                foreach (ChatModelInfo model in defaultModels)
                {
                    result.Add(model.Clone());
                }
            }

            if (_customChatModels.TryGetValue(providerType_, out List<ChatModelInfo> customModels))
            {
                foreach (ChatModelInfo model in customModels)
                {
                    bool exists = false;
                    foreach (ChatModelInfo existing in result)
                    {
                        if (existing.Id == model.Id)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        ChatModelInfo clone = model.Clone();
                        clone.IsCustom = true;
                        result.Add(clone);
                    }
                }
            }

            return result;
        }

        public ChatModelInfo GetChatModelInfo(ChatEditorProviderType providerType_, string modelId_)
        {
            List<ChatModelInfo> models = GetChatModelInfos(providerType_);
            foreach (ChatModelInfo model in models)
            {
                if (model.Id == modelId_)
                    return model;
            }
            return null;
        }

        public List<ChatModelInfo> GetCustomChatModels(ChatEditorProviderType providerType_)
        {
            if (_customChatModels.TryGetValue(providerType_, out List<ChatModelInfo> models))
            {
                List<ChatModelInfo> result = new List<ChatModelInfo>();
                foreach (ChatModelInfo model in models)
                {
                    result.Add(model.Clone());
                }
                return result;
            }
            return new List<ChatModelInfo>();
        }

        public void AddCustomChatModel(ChatEditorProviderType providerType_, ChatModelInfo chatModelInfo_)
        {
            if (chatModelInfo_ == null || string.IsNullOrWhiteSpace(chatModelInfo_.Id))
                return;

            if (!_customChatModels.ContainsKey(providerType_))
            {
                _customChatModels[providerType_] = new List<ChatModelInfo>();
            }

            foreach (ChatModelInfo existing in _customChatModels[providerType_])
            {
                if (existing.Id == chatModelInfo_.Id)
                    return;
            }

            chatModelInfo_.IsCustom = true;
            _customChatModels[providerType_].Add(chatModelInfo_);
        }

        public void AddCustomChatModel(ChatEditorProviderType providerType_, string modelId_)
        {
            AddCustomChatModel(
                providerType_,
                new ChatModelInfo(modelId_, modelId_, "", null, null, 0, null, GetModelsUrl(providerType_), true));
        }

        public void RemoveCustomChatModel(ChatEditorProviderType providerType_, string modelId_)
        {
            if (_customChatModels.TryGetValue(providerType_, out List<ChatModelInfo> models))
            {
                for (int i = models.Count - 1; i >= 0; i--)
                {
                    if (models[i].Id == modelId_)
                    {
                        models.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void SetCustomChatModels(ChatEditorProviderType providerType_, List<ChatModelInfo> models_)
        {
            _customChatModels[providerType_] = models_ != null ? new List<ChatModelInfo>(models_) : new List<ChatModelInfo>();
        }

        public void LoadFromStorage(EditorDataStorage storage_)
        {
            if (storage_ == null)
                return;

            LoadDefaultPresets();

            string customFilePath = storage_.GetString(EditorDataStorageKeys.KEY_CUSTOM_PRESETS_FILE_PATH);
            if (!string.IsNullOrEmpty(customFilePath))
            {
                LoadCustomPresets(customFilePath);
            }
        }

        public void SaveToStorage(EditorDataStorage storage_)
        {
            if (storage_ == null)
                return;

            if (!string.IsNullOrEmpty(_customPresetsFilePath))
            {
                storage_.SetString(EditorDataStorageKeys.KEY_CUSTOM_PRESETS_FILE_PATH, _customPresetsFilePath);
                SaveCustomPresets();
            }
            storage_.Save();
        }

        private List<ChatModelInfo> GetDefaultModels(ChatEditorProviderType providerType_)
        {
            if (_defaultChatModels.TryGetValue(providerType_, out List<ChatModelInfo> models))
                return models;

            return new List<ChatModelInfo>();
        }
    }
}
