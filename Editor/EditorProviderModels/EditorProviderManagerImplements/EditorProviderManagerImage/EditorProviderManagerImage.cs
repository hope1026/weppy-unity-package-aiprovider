using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Weppy.AIProvider.Editor
{
    public class EditorProviderManagerImage :
        EditorProviderManagerInterface<ImageEditorProviderType, ImageModelInfo>,
        EditorProviderExecutionPathSupport<ImageEditorProviderType>
    {
        private readonly EditorDataStorage _storage;
        private readonly ImageModelPresets _imageModelPresets;
        private readonly Dictionary<ImageEditorProviderType, bool> _providerEnabledStates = new Dictionary<ImageEditorProviderType, bool>();
        private readonly Dictionary<ImageEditorProviderType, List<string>> _selectedModelIdsByProvider = new Dictionary<ImageEditorProviderType, List<string>>();
        private readonly Dictionary<ImageEditorProviderType, string> _primaryModelIds = new Dictionary<ImageEditorProviderType, string>();

        public EditorProviderManagerImage(EditorDataStorage storage_)
        {
            _storage = storage_;
            _imageModelPresets = new ImageModelPresets();
            _imageModelPresets.LoadFromStorage(_storage);
        }

        public List<ImageEditorProviderType> GetProviders()
        {
            List<ImageEditorProviderType> providers = new List<ImageEditorProviderType>();
            foreach (ImageEditorProviderType providerType in Enum.GetValues(typeof(ImageEditorProviderType)))
            {
                if (providerType == ImageEditorProviderType.NONE)
                    continue;

                providers.Add(providerType);
            }

            return providers;
        }

        public bool IsProviderEnabled(ImageEditorProviderType providerType_)
        {
            return _providerEnabledStates.GetValueOrDefault(providerType_, true);
        }

        public void SetProviderEnabled(ImageEditorProviderType providerType_, bool enabled_)
        {
            _providerEnabledStates[providerType_] = enabled_;
        }

        public List<ImageModelInfo> GetAllModelInfos(ImageEditorProviderType providerType_)
        {
            return _imageModelPresets.GetImageModelInfos(providerType_);
        }

        public ImageModelInfo GetModelInfo(ImageEditorProviderType providerType_, string modelId_)
        {
            return _imageModelPresets.GetImageModelInfo(providerType_, modelId_);
        }

        public string GetPrimarySelectedModelId(ImageEditorProviderType providerType_)
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

        public IReadOnlyList<string> GetSelectedModelIds(ImageEditorProviderType providerType_)
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
                string fallbackId = _imageModelPresets.GetDefaultImageModel(providerType_);
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

        public void SetSelectedModelId(ImageEditorProviderType providerType_, string modelId_)
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

        public void SetPrimaryModelId(ImageEditorProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            IReadOnlyList<string> selectedIds = GetSelectedModelIds(providerType_);
            if (!selectedIds.Contains(modelId_))
                return;

            _primaryModelIds[providerType_] = modelId_;
            SaveSelectedModelId(providerType_, modelId_);
        }

        public void SetSelectedModelIds(ImageEditorProviderType providerType_, List<string> modelIds_)
        {
            List<string> normalized = NormalizeModelIdList(providerType_, modelIds_);
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

        public void AddCustomModel(ImageEditorProviderType providerType_, ImageModelInfo modelInfo_)
        {
            _imageModelPresets.AddCustomImageModel(providerType_, modelInfo_);
            _imageModelPresets.SaveToStorage(_storage);
        }

        public void RemoveCustomModel(ImageEditorProviderType providerType_, string modelId_)
        {
            _imageModelPresets.RemoveCustomImageModel(providerType_, modelId_);
            _imageModelPresets.SaveToStorage(_storage);
        }

        private List<string> GetStoredSelectedModelIds(ImageEditorProviderType providerType_)
        {
            List<string> modelIds = new List<string>();
            if (_storage == null)
                return modelIds;

            string listKey = EditorDataStorageKeys.GetSelectedImageModelListKey(providerType_);
            if (string.IsNullOrEmpty(listKey))
                return modelIds;

            string serialized = _storage.GetString(listKey);
            if (string.IsNullOrEmpty(serialized))
                return modelIds;

            return ParseModelIdList(serialized);
        }

        private string GetStoredSelectedModelId(ImageEditorProviderType providerType_)
        {
            if (_storage == null)
                return string.Empty;

            string key = EditorDataStorageKeys.GetSelectedImageModelKey(providerType_);
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return _storage.GetString(key);
        }

        private void SaveSelectedModelIds(ImageEditorProviderType providerType_, List<string> modelIds_)
        {
            if (_storage == null)
                return;

            string listKey = EditorDataStorageKeys.GetSelectedImageModelListKey(providerType_);
            if (string.IsNullOrEmpty(listKey))
                return;

            string serialized = SerializeModelIdList(modelIds_);
            _storage.SetString(listKey, serialized);
            _storage.Save();
        }

        private void SaveSelectedModelId(ImageEditorProviderType providerType_, string modelId_)
        {
            if (_storage == null)
                return;

            string key = EditorDataStorageKeys.GetSelectedImageModelKey(providerType_);
            if (string.IsNullOrEmpty(key))
                return;

            _storage.SetString(key, modelId_ ?? string.Empty);
            _storage.Save();
        }

        private List<string> NormalizeModelIdList(ImageEditorProviderType providerType_, List<string> modelIds_)
        {
            List<string> normalized = new List<string>();
            if (modelIds_ == null)
                return normalized;

            HashSet<string> availableModelIds = new HashSet<string>();
            List<ImageModelInfo> availableModels = _imageModelPresets.GetImageModelInfos(providerType_);
            foreach (ImageModelInfo modelInfo in availableModels)
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

        public string GetDefaultImageModel(ImageEditorProviderType providerType_)
        {
            return _imageModelPresets.GetDefaultImageModel(providerType_);
        }

        public string GetModelsUrl(ImageEditorProviderType providerType_)
        {
            return _imageModelPresets.GetModelsUrl(providerType_);
        }

        public ProviderExecutionPathType GetExecutionPathType(ImageEditorProviderType providerType_)
        {
            return providerType_ == ImageEditorProviderType.CODEX_APP
                ? ProviderExecutionPathType.APP
                : ProviderExecutionPathType.NONE;
        }

        public string GetExecutablePath(ImageEditorProviderType providerType_)
        {
            return EditorDataStorageKeys.GetImageAppExecutablePath(_storage, providerType_);
        }

        public void SetExecutablePath(ImageEditorProviderType providerType_, string path_)
        {
            EditorDataStorageKeys.SetImageAppExecutablePath(_storage, providerType_, path_);
        }

        public string AutoDetectExecutablePath(ImageEditorProviderType providerType_)
        {
            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return string.Empty;

            return CodexAppWrapper.FindCodexAppExecutablePath();
        }

        public string GetExecutableInstallGuideUrl(ImageEditorProviderType providerType_)
        {
            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return string.Empty;

            return "https://developers.openai.com/codex/cli/reference#app-server";
        }

        public bool SupportsNodeExecutablePath(ImageEditorProviderType providerType_)
        {
            return providerType_ == ImageEditorProviderType.CODEX_APP;
        }

        public string GetNodeExecutablePath(ImageEditorProviderType providerType_)
        {
            return EditorDataStorageKeys.GetImageAppNodeExecutablePath(_storage, providerType_);
        }

        public void SetNodeExecutablePath(ImageEditorProviderType providerType_, string path_)
        {
            EditorDataStorageKeys.SetImageAppNodeExecutablePath(_storage, providerType_, path_);
        }

        public string AutoDetectNodeExecutablePath(ImageEditorProviderType providerType_)
        {
            if (providerType_ != ImageEditorProviderType.CODEX_APP)
                return string.Empty;

            return CodexAppWrapper.FindNodeExecutablePath();
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
