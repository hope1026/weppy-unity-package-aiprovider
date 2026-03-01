using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Weppy.AIProvider.Editor
{
    public class BgRemovalModelPresets
    {
        public const string DEFAULT_PRESETS_RESOURCE_PATH = "DefaultBgRemovalModelPresets";

        public const string REMOVE_BG_API_URL = "https://www.remove.bg/api";

        private readonly Dictionary<BgRemovalProviderType, List<BgRemovalModelInfo>> _defaultBgRemovalModels =
            new Dictionary<BgRemovalProviderType, List<BgRemovalModelInfo>>();

        private readonly Dictionary<BgRemovalProviderType, List<BgRemovalModelInfo>> _customBgRemovalModels =
            new Dictionary<BgRemovalProviderType, List<BgRemovalModelInfo>>();

        private bool _defaultPresetsLoaded = false;
        private string _customPresetsFilePath = "";

        public string CustomPresetsFilePath { get => _customPresetsFilePath; set => _customPresetsFilePath = value; }

        public void LoadDefaultPresets()
        {
            if (_defaultPresetsLoaded)
                return;

            _defaultBgRemovalModels.Clear();

            string assetPath = EditorPaths.RESOURCES_PATH + DEFAULT_PRESETS_RESOURCE_PATH + ".json";
            TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (jsonAsset != null)
            {
                try
                {
                    DefaultBgRemovalPresetsJson presetsJson = JsonUtility.FromJson<DefaultBgRemovalPresetsJson>(jsonAsset.text);
                    if (presetsJson != null)
                    {
                        LoadBgRemovalProvidersFromJson(presetsJson.BgRemovalProviders, _defaultBgRemovalModels, false);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BgRemovalModelPresets] Failed to parse default presets JSON: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[BgRemovalModelPresets] Default presets JSON not found in Resources");
            }

            _defaultPresetsLoaded = true;
        }

        public void LoadCustomPresets(string filePath_)
        {
            _customPresetsFilePath = filePath_;
            _customBgRemovalModels.Clear();

            if (string.IsNullOrEmpty(filePath_) || !File.Exists(filePath_))
                return;

            try
            {
                string json = File.ReadAllText(filePath_);
                CustomPresetsJson customJson = JsonUtility.FromJson<CustomPresetsJson>(json);
                if (customJson != null)
                {
                    if (customJson.customBgRemovalProviders != null)
                    {
                        LoadBgRemovalProvidersFromJson(customJson.customBgRemovalProviders, _customBgRemovalModels, true);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BgRemovalModelPresets] Failed to load custom presets from {filePath_}: {e.Message}");
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
                foreach (KeyValuePair<BgRemovalProviderType, List<BgRemovalModelInfo>> kvp in _customBgRemovalModels)
                {
                    if (kvp.Value.Count > 0)
                    {
                        BgRemovalModelsJson bgRemovalJson = new BgRemovalModelsJson
                        {
                            provider = kvp.Key.ToString(),
                            modelsUrl = GetModelsUrl(kvp.Key)
                        };
                        foreach (BgRemovalModelInfo model in kvp.Value)
                        {
                            bgRemovalJson.models.Add(new BgRemovalModelJsonEntry(model));
                        }
                        customJson.customBgRemovalProviders.Add(bgRemovalJson);
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
                Debug.LogError($"[BgRemovalModelPresets] Failed to save custom presets: {e.Message}");
            }
        }

        private static void LoadBgRemovalProvidersFromJson(
            List<BgRemovalModelsJson> providers_,
            Dictionary<BgRemovalProviderType, List<BgRemovalModelInfo>> target_,
            bool isCustom_)
        {
            if (providers_ == null)
                return;

            foreach (BgRemovalModelsJson providerJson in providers_)
            {
                if (Enum.TryParse(providerJson.provider, out BgRemovalProviderType providerType))
                {
                    if (!target_.ContainsKey(providerType))
                    {
                        target_[providerType] = new List<BgRemovalModelInfo>();
                    }

                    foreach (BgRemovalModelJsonEntry modelEntry in providerJson.models)
                    {
                        BgRemovalModelInfo modelInfo = modelEntry.ToBgRemovalModelInfo(isCustom_);
                        target_[providerType].Add(modelInfo);
                    }
                }
            }
        }

        public string GetModelsUrl(BgRemovalProviderType providerType_)
        {
            return providerType_ switch
            {
                BgRemovalProviderType.REMOVE_BG => REMOVE_BG_API_URL,
                _ => ""
            };
        }

        public string GetDefaultBgRemovalModel(BgRemovalProviderType providerType_)
        {
            LoadDefaultPresets();
            if (_defaultBgRemovalModels.TryGetValue(providerType_, out List<BgRemovalModelInfo> models) && models.Count > 0)
            {
                foreach (BgRemovalModelInfo model in models)
                {
                    return model.Id;
                }
            }
            return "";
        }

        public List<BgRemovalModelInfo> GetBgRemovalModelInfos(BgRemovalProviderType providerType_)
        {
            LoadDefaultPresets();
            List<BgRemovalModelInfo> result = new List<BgRemovalModelInfo>();

            if (_defaultBgRemovalModels.TryGetValue(providerType_, out List<BgRemovalModelInfo> defaultModels))
            {
                foreach (BgRemovalModelInfo model in defaultModels)
                {
                    result.Add(model.Clone());
                }
            }

            if (_customBgRemovalModels.TryGetValue(providerType_, out List<BgRemovalModelInfo> customModels))
            {
                foreach (BgRemovalModelInfo model in customModels)
                {
                    bool exists = false;
                    foreach (BgRemovalModelInfo existing in result)
                    {
                        if (existing.Id == model.Id)
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        BgRemovalModelInfo clone = model.Clone();
                        clone.IsCustom = true;
                        result.Add(clone);
                    }
                }
            }

            return result;
        }

        public BgRemovalModelInfo GetBgRemovalModelInfo(BgRemovalProviderType providerType_, string modelId_)
        {
            List<BgRemovalModelInfo> models = GetBgRemovalModelInfos(providerType_);
            foreach (BgRemovalModelInfo model in models)
            {
                if (model.Id == modelId_)
                    return model;
            }
            return null;
        }

        public void AddCustomBgRemovalModel(BgRemovalProviderType providerType_, BgRemovalModelInfo modelInfo_)
        {
            if (modelInfo_ == null || string.IsNullOrWhiteSpace(modelInfo_.Id))
                return;

            if (!_customBgRemovalModels.ContainsKey(providerType_))
            {
                _customBgRemovalModels[providerType_] = new List<BgRemovalModelInfo>();
            }

            foreach (BgRemovalModelInfo existing in _customBgRemovalModels[providerType_])
            {
                if (existing.Id == modelInfo_.Id)
                    return;
            }

            modelInfo_.IsCustom = true;
            _customBgRemovalModels[providerType_].Add(modelInfo_);
        }

        public void AddCustomBgRemovalModel(BgRemovalProviderType providerType_, string modelId_)
        {
            AddCustomBgRemovalModel(providerType_, new BgRemovalModelInfo(modelId_, modelId_, "", null, GetModelsUrl(providerType_), true));
        }

        public void RemoveCustomBgRemovalModel(BgRemovalProviderType providerType_, string modelId_)
        {
            if (_customBgRemovalModels.TryGetValue(providerType_, out List<BgRemovalModelInfo> models))
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
