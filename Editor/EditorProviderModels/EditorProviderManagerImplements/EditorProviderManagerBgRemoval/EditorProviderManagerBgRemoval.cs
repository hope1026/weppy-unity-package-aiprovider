using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Weppy.AIProvider.Editor
{
    public class EditorProviderManagerBgRemoval : EditorProviderManagerInterface<BgRemovalProviderType, BgRemovalModelInfo>
    {
        private readonly EditorDataStorage _storage;
        private readonly BgRemovalModelPresets _bgRemovalModelPresets;
        private readonly Dictionary<BgRemovalProviderType, bool> _providerEnabledStates = new Dictionary<BgRemovalProviderType, bool>();
        private readonly Dictionary<BgRemovalProviderType, List<string>> _selectedModelIdsByProvider = new Dictionary<BgRemovalProviderType, List<string>>();
        private readonly Dictionary<BgRemovalProviderType, string> _primaryModelIds = new Dictionary<BgRemovalProviderType, string>();

        public EditorProviderManagerBgRemoval(EditorDataStorage storage_)
        {
            _storage = storage_;
            _bgRemovalModelPresets = new BgRemovalModelPresets();
            _bgRemovalModelPresets.LoadFromStorage(_storage);
        }

        public List<BgRemovalProviderType> GetProviders()
        {
            List<BgRemovalProviderType> providers = new List<BgRemovalProviderType>();
            foreach (BgRemovalProviderType providerType in Enum.GetValues(typeof(BgRemovalProviderType)))
            {
                if (providerType == BgRemovalProviderType.NONE)
                    continue;

                providers.Add(providerType);
            }

            return providers;
        }

        public bool IsProviderEnabled(BgRemovalProviderType providerType_)
        {
            return _providerEnabledStates.GetValueOrDefault(providerType_, true);
        }

        public void SetProviderEnabled(BgRemovalProviderType providerType_, bool enabled_)
        {
            _providerEnabledStates[providerType_] = enabled_;
        }

        public List<BgRemovalModelInfo> GetAllModelInfos(BgRemovalProviderType providerType_)
        {
            return _bgRemovalModelPresets.GetBgRemovalModelInfos(providerType_);
        }

        public BgRemovalModelInfo GetModelInfo(BgRemovalProviderType providerType_, string modelId_)
        {
            return _bgRemovalModelPresets.GetBgRemovalModelInfo(providerType_, modelId_);
        }

        public string GetPrimarySelectedModelId(BgRemovalProviderType providerType_)
        {
            IReadOnlyList<string> selectedIds = GetSelectedModelIds(providerType_);
            if (selectedIds.Count == 0)
                return string.Empty;

            if (_primaryModelIds.TryGetValue(providerType_, out string primaryId) &&
                !string.IsNullOrEmpty(primaryId) &&
                selectedIds.Contains(primaryId))
            {
                return primaryId;
            }

            return selectedIds[0];
        }

        public IReadOnlyList<string> GetSelectedModelIds(BgRemovalProviderType providerType_)
        {
            if (_selectedModelIdsByProvider.TryGetValue(providerType_, out List<string> cached))
            {
                return cached;
            }

            List<string> modelIds = GetStoredSelectedModelIds(providerType_);
            if (modelIds.Count == 0)
            {
                string singleId = GetStoredSelectedModelId(providerType_);
                if (!string.IsNullOrEmpty(singleId))
                    modelIds.Add(singleId);
            }

            List<string> normalized = NormalizeModelIdList(providerType_, modelIds);
            if (normalized.Count == 0)
            {
                string fallbackId = _bgRemovalModelPresets.GetDefaultBgRemovalModel(providerType_);
                if (!string.IsNullOrEmpty(fallbackId))
                    normalized.Add(fallbackId);
            }

            _selectedModelIdsByProvider[providerType_] = normalized;
            if (normalized.Count > 0)
            {
                string storedPrimaryId = GetStoredSelectedModelId(providerType_);
                if (!string.IsNullOrEmpty(storedPrimaryId) && normalized.Contains(storedPrimaryId))
                {
                    _primaryModelIds[providerType_] = storedPrimaryId;
                }
                else
                {
                    _primaryModelIds[providerType_] = normalized[0];
                }
            }

            return normalized;
        }

        public void SetSelectedModelId(BgRemovalProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            IReadOnlyList<string> existingModelIds = GetSelectedModelIds(providerType_);
            if (existingModelIds.Contains(modelId_))
            {
                SetPrimaryModelId(providerType_, modelId_);
            }
            else
            {
                List<string> modelIds = new List<string>(existingModelIds) { modelId_ };
                _primaryModelIds[providerType_] = modelId_;
                SetSelectedModelIds(providerType_, modelIds);
            }
        }

        public void SetPrimaryModelId(BgRemovalProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            IReadOnlyList<string> selectedIds = GetSelectedModelIds(providerType_);
            if (!selectedIds.Contains(modelId_))
                return;

            _primaryModelIds[providerType_] = modelId_;
            SaveSelectedModelId(providerType_, modelId_);
        }

        public void SetSelectedModelIds(BgRemovalProviderType providerType_, List<string> modelIds_)
        {
            IReadOnlyList<string> existingModelIds = GetSelectedModelIds(providerType_);
            List<string> combined = new List<string>(existingModelIds);

            if (modelIds_ != null)
            {
                foreach (string modelId in modelIds_)
                {
                    if (!string.IsNullOrEmpty(modelId) && !combined.Contains(modelId))
                    {
                        combined.Add(modelId);
                    }
                }
            }

            List<string> normalized = NormalizeModelIdList(providerType_, combined);
            _selectedModelIdsByProvider[providerType_] = normalized;

            string primaryId = string.Empty;
            if (normalized.Count > 0)
            {
                if (_primaryModelIds.TryGetValue(providerType_, out string existingPrimary) &&
                    !string.IsNullOrEmpty(existingPrimary) &&
                    normalized.Contains(existingPrimary))
                {
                    primaryId = existingPrimary;
                }
                else
                {
                    primaryId = normalized[0];
                }
                _primaryModelIds[providerType_] = primaryId;
            }

            SaveSelectedModelIds(providerType_, normalized);
            SaveSelectedModelId(providerType_, primaryId);
        }

        public void AddCustomModel(BgRemovalProviderType providerType_, BgRemovalModelInfo modelInfo_)
        {
            _bgRemovalModelPresets.AddCustomBgRemovalModel(providerType_, modelInfo_);
            _bgRemovalModelPresets.SaveToStorage(_storage);
        }

        public void RemoveCustomModel(BgRemovalProviderType providerType_, string modelId_)
        {
            _bgRemovalModelPresets.RemoveCustomBgRemovalModel(providerType_, modelId_);
            _bgRemovalModelPresets.SaveToStorage(_storage);
        }

        public string GetDefaultBgRemovalModel(BgRemovalProviderType providerType_)
        {
            return _bgRemovalModelPresets.GetDefaultBgRemovalModel(providerType_);
        }

        public string GetModelsUrl(BgRemovalProviderType providerType_)
        {
            return _bgRemovalModelPresets.GetModelsUrl(providerType_);
        }

        private List<string> GetStoredSelectedModelIds(BgRemovalProviderType providerType_)
        {
            List<string> modelIds = new List<string>();
            if (_storage == null)
                return modelIds;

            string listKey = EditorDataStorageKeys.GetSelectedBgRemovalModelListKey(providerType_);
            if (string.IsNullOrEmpty(listKey))
                return modelIds;

            string serialized = _storage.GetString(listKey);
            if (string.IsNullOrEmpty(serialized))
                return modelIds;

            return ParseModelIdList(serialized);
        }

        private string GetStoredSelectedModelId(BgRemovalProviderType providerType_)
        {
            if (_storage == null)
                return string.Empty;

            string key = EditorDataStorageKeys.GetSelectedBgRemovalModelKey(providerType_);
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return _storage.GetString(key);
        }

        private void SaveSelectedModelIds(BgRemovalProviderType providerType_, List<string> modelIds_)
        {
            if (_storage == null)
                return;

            string listKey = EditorDataStorageKeys.GetSelectedBgRemovalModelListKey(providerType_);
            if (string.IsNullOrEmpty(listKey))
                return;

            string serialized = SerializeModelIdList(modelIds_);
            _storage.SetString(listKey, serialized);
            _storage.Save();
        }

        private void SaveSelectedModelId(BgRemovalProviderType providerType_, string modelId_)
        {
            if (_storage == null)
                return;

            string key = EditorDataStorageKeys.GetSelectedBgRemovalModelKey(providerType_);
            if (string.IsNullOrEmpty(key))
                return;

            _storage.SetString(key, modelId_ ?? string.Empty);
            _storage.Save();
        }

        private List<string> NormalizeModelIdList(BgRemovalProviderType providerType_, List<string> modelIds_)
        {
            List<string> normalized = new List<string>();
            if (modelIds_ == null)
                return normalized;

            HashSet<string> availableModelIds = new HashSet<string>();
            List<BgRemovalModelInfo> availableModels = _bgRemovalModelPresets.GetBgRemovalModelInfos(providerType_);
            foreach (BgRemovalModelInfo modelInfo in availableModels)
            {
                if (!string.IsNullOrEmpty(modelInfo.Id))
                {
                    availableModelIds.Add(modelInfo.Id);
                }
            }

            foreach (string modelId in modelIds_)
            {
                if (string.IsNullOrEmpty(modelId))
                    continue;

                if (availableModelIds.Count > 0 && !availableModelIds.Contains(modelId))
                    continue;

                if (!normalized.Contains(modelId))
                {
                    normalized.Add(modelId);
                }
            }

            return normalized;
        }

        private List<string> ParseModelIdList(string serialized_)
        {
            List<string> modelIds = new List<string>();
            if (string.IsNullOrEmpty(serialized_))
                return modelIds;

            string[] parts = serialized_.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !modelIds.Contains(trimmed))
                {
                    modelIds.Add(trimmed);
                }
            }

            return modelIds;
        }

        private string SerializeModelIdList(List<string> modelIds_)
        {
            if (modelIds_ == null || modelIds_.Count == 0)
                return string.Empty;

            return string.Join("|", modelIds_);
        }
    }
}