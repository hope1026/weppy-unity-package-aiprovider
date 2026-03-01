using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Weppy.AIProvider.Editor
{
    public class ImageModelPresets
    {
        public const string DEFAULT_PRESETS_RESOURCE_PATH = "DefaultImageModelPresets";

        public const string OPENAI_IMAGE_MODELS_URL = "https://platform.openai.com/docs/models";
        public const string CODEX_APP_IMAGE_MODELS_URL = "https://developers.openai.com/codex/cli/reference#app-server";
        public const string GOOGLE_GEMINI_IMAGE_MODELS_URL = "https://ai.google.dev/gemini-api/docs/nanobanana";
        public const string GOOGLE_IMAGEN_MODELS_URL = "https://ai.google.dev/gemini-api/docs/imagen";
        public const string HUGGINGFACE_IMAGE_MODELS_URL = "https://huggingface.co/models?pipeline_tag=text-to-image";
        public const string OPENROUTER_IMAGE_MODELS_URL = "https://openrouter.ai/models";

        private readonly Dictionary<ImageEditorProviderType, List<ImageModelInfo>> _defaultImageModels =
            new Dictionary<ImageEditorProviderType, List<ImageModelInfo>>();
        private readonly Dictionary<ImageEditorProviderType, List<ImageModelInfo>> _customImageModels =
            new Dictionary<ImageEditorProviderType, List<ImageModelInfo>>();

        private bool _defaultPresetsLoaded = false;
        private string _customPresetsFilePath = "";

        public void LoadDefaultPresets()
        {
            if (_defaultPresetsLoaded)
                return;

            _defaultImageModels.Clear();

            string assetPath = EditorPaths.RESOURCES_PATH + DEFAULT_PRESETS_RESOURCE_PATH + ".json";
            TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (jsonAsset != null)
            {
                try
                {
                    DefaultImagePresetsJson presetsJson = JsonUtility.FromJson<DefaultImagePresetsJson>(jsonAsset.text);
                    if (presetsJson != null)
                    {
                        LoadImageProvidersFromJson(presetsJson.imageProviders, _defaultImageModels, false);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ImageModelPresets] Failed to parse default presets JSON: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[ImageModelPresets] Default presets JSON not found in Resources");
            }

            _defaultPresetsLoaded = true;
        }

        public void LoadCustomPresets(string filePath_)
        {
            _customPresetsFilePath = filePath_;
            _customImageModels.Clear();

            if (string.IsNullOrEmpty(filePath_) || !File.Exists(filePath_))
                return;

            try
            {
                string json = File.ReadAllText(filePath_);
                CustomPresetsJson customJson = JsonUtility.FromJson<CustomPresetsJson>(json);
                if (customJson != null)
                {
                    if (customJson.customImageProviders != null)
                    {
                        LoadImageProvidersFromJson(customJson.customImageProviders, _customImageModels, true);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ImageModelPresets] Failed to load custom presets from {filePath_}: {e.Message}");
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
                foreach (KeyValuePair<ImageEditorProviderType, List<ImageModelInfo>> kvp in _customImageModels)
                {
                    if (kvp.Value.Count > 0)
                    {
                        ImageModelsJson generationJson = new ImageModelsJson
                        {
                            provider = kvp.Key.ToString(),
                            modelsUrl = GetModelsUrl(kvp.Key)
                        };
                        foreach (ImageModelInfo model in kvp.Value)
                        {
                            generationJson.models.Add(new ImageModelJsonEntry(model));
                        }
                        customJson.customImageProviders.Add(generationJson);
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
                Debug.LogError($"[ImageModelPresets] Failed to save custom presets: {e.Message}");
            }
        }

        private static void LoadImageProvidersFromJson(List<ImageModelsJson> providers_, Dictionary<ImageEditorProviderType, List<ImageModelInfo>> target_, bool isCustom_)
        {
            if (providers_ == null)
                return;

            foreach (ImageModelsJson providerJson in providers_)
            {
                if (Enum.TryParse(providerJson.provider, out ImageEditorProviderType providerType))
                {
                    if (!target_.ContainsKey(providerType))
                    {
                        target_[providerType] = new List<ImageModelInfo>();
                    }

                    foreach (ImageModelJsonEntry modelEntry in providerJson.models)
                    {
                        ImageModelInfo generationModelInfo = modelEntry.ToImageModelInfo(isCustom_);
                        target_[providerType].Add(generationModelInfo);
                    }
                }
            }
        }

        public string GetModelsUrl(ImageEditorProviderType generationProviderType_)
        {
            return generationProviderType_ switch
            {
                ImageEditorProviderType.OPEN_AI => OPENAI_IMAGE_MODELS_URL,
                ImageEditorProviderType.CODEX_APP => CODEX_APP_IMAGE_MODELS_URL,
                ImageEditorProviderType.GOOGLE_GEMINI => GOOGLE_GEMINI_IMAGE_MODELS_URL,
                ImageEditorProviderType.GOOGLE_IMAGEN => GOOGLE_IMAGEN_MODELS_URL,
                ImageEditorProviderType.OPEN_ROUTER => OPENROUTER_IMAGE_MODELS_URL,
                _ => ""
            };
        }

        public string GetDefaultImageModel(ImageEditorProviderType generationProviderType_)
        {
            LoadDefaultPresets();
            if (_defaultImageModels.TryGetValue(generationProviderType_, out List<ImageModelInfo> models) && models.Count > 0)
            {
                foreach (ImageModelInfo model in models)
                {
                    return model.Id;
                }
            }
            return "";
        }

        public List<ImageModelInfo> GetImageModelInfos(ImageEditorProviderType generationProviderType_)
        {
            LoadDefaultPresets();
            List<ImageModelInfo> result = new List<ImageModelInfo>();

            if (_defaultImageModels.TryGetValue(generationProviderType_, out List<ImageModelInfo> defaultModels))
            {
                foreach (ImageModelInfo model in defaultModels)
                {
                    result.Add(model.Clone());
                }
            }

            if (_customImageModels.TryGetValue(generationProviderType_, out List<ImageModelInfo> customModels))
            {
                foreach (ImageModelInfo model in customModels)
                {
                    bool exists = false;
                    foreach (ImageModelInfo existing in result)
                    {
                        if (existing.Id == model.Id)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        ImageModelInfo clone = model.Clone();
                        clone.IsCustom = true;
                        result.Add(clone);
                    }
                }
            }

            return result;
        }

        public ImageModelInfo GetImageModelInfo(ImageEditorProviderType generationProviderType_, string modelId_)
        {
            List<ImageModelInfo> models = GetImageModelInfos(generationProviderType_);
            foreach (ImageModelInfo model in models)
            {
                if (model.Id == modelId_)
                    return model;
            }
            return null;
        }

        public List<ImageModelInfo> GetCustomImageModels(ImageEditorProviderType generationProviderType_)
        {
            if (_customImageModels.TryGetValue(generationProviderType_, out List<ImageModelInfo> models))
            {
                List<ImageModelInfo> result = new List<ImageModelInfo>();
                foreach (ImageModelInfo model in models)
                {
                    result.Add(model.Clone());
                }
                return result;
            }
            return new List<ImageModelInfo>();
        }

        public void AddCustomImageModel(ImageEditorProviderType generationProviderType_, ImageModelInfo generationModelInfo_)
        {
            if (generationModelInfo_ == null || string.IsNullOrWhiteSpace(generationModelInfo_.Id))
                return;

            if (!_customImageModels.ContainsKey(generationProviderType_))
            {
                _customImageModels[generationProviderType_] = new List<ImageModelInfo>();
            }

            foreach (ImageModelInfo existing in _customImageModels[generationProviderType_])
            {
                if (existing.Id == generationModelInfo_.Id)
                    return;
            }

            generationModelInfo_.IsCustom = true;
            _customImageModels[generationProviderType_].Add(generationModelInfo_);
        }

        public void AddCustomImageModel(ImageEditorProviderType generationProviderType_, string modelId_)
        {
            AddCustomImageModel(generationProviderType_, new ImageModelInfo(modelId_, modelId_, "", null, GetModelsUrl(generationProviderType_), true));
        }

        public void RemoveCustomImageModel(ImageEditorProviderType generationProviderType_, string modelId_)
        {
            if (_customImageModels.TryGetValue(generationProviderType_, out List<ImageModelInfo> models))
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

        public void SetCustomImageModels(ImageEditorProviderType generationProviderType_, List<ImageModelInfo> models_)
        {
            _customImageModels[generationProviderType_] = models_ != null ? new List<ImageModelInfo>(models_) : new List<ImageModelInfo>();
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

    }
}
