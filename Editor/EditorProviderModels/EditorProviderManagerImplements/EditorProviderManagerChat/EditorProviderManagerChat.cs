using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Weppy.AIProvider.Chat.Editor
{
    public class EditorProviderManagerChat : EditorProviderManagerInterface<ChatEditorProviderType, ChatModelInfo>
    {
        public event Action<ChatEditorProviderType> OnSelectedModelChanged;

        private readonly EditorDataStorage _storage;
        private readonly ChatModelPresets _chatModelPresets;
        private readonly Dictionary<ChatEditorProviderType, bool> _providerEnabledStates = new Dictionary<ChatEditorProviderType, bool>();
        private readonly Dictionary<ChatEditorProviderType, List<string>> _selectedModelIdsByProvider = new Dictionary<ChatEditorProviderType, List<string>>();
        private readonly Dictionary<ChatEditorProviderType, string> _primaryModelIds = new Dictionary<ChatEditorProviderType, string>();

        public EditorProviderManagerChat(EditorDataStorage storage_)
        {
            _storage = storage_;
            _chatModelPresets = new ChatModelPresets();
            _chatModelPresets.LoadFromStorage(_storage);
        }

        public List<ChatEditorProviderType> GetProviders()
        {
            List<ChatEditorProviderType> providers = new List<ChatEditorProviderType>();
            foreach (ChatEditorProviderType providerType in Enum.GetValues(typeof(ChatEditorProviderType)))
            {
                if (providerType == ChatEditorProviderType.NONE)
                    continue;

                providers.Add(providerType);
            }

            return providers;
        }

        public bool IsProviderEnabled(ChatEditorProviderType providerType_)
        {
            return _providerEnabledStates.GetValueOrDefault(providerType_, true);
        }

        public void SetProviderEnabled(ChatEditorProviderType providerType_, bool enabled_)
        {
            _providerEnabledStates[providerType_] = enabled_;
        }

        public List<ChatModelInfo> GetAllModelInfos(ChatEditorProviderType providerType_)
        {
            return _chatModelPresets.GetChatModelInfos(providerType_);
        }

        public ChatModelInfo GetModelInfo(ChatEditorProviderType providerType_, string modelId_)
        {
            return _chatModelPresets.GetChatModelInfo(providerType_, modelId_);
        }

        public string GetPrimarySelectedModelId(ChatEditorProviderType providerType_)
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

        public IReadOnlyList<string> GetSelectedModelIds(ChatEditorProviderType providerType_)
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
                string fallbackId = _chatModelPresets.GetDefaultChatModel(providerType_);
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

        public void SetSelectedModelId(ChatEditorProviderType providerType_, string modelId_)
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

        public void SetPrimaryModelId(ChatEditorProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            IReadOnlyList<string> selectedIds = GetSelectedModelIds(providerType_);
            if (!selectedIds.Contains(modelId_))
                return;

            _primaryModelIds[providerType_] = modelId_;
            SaveSelectedPrimaryModelId(providerType_, modelId_);
        }

        public void SetSelectedModelIds(ChatEditorProviderType providerType_, List<string> modelIds_)
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
            SaveSelectedPrimaryModelId(providerType_, primaryId);
        }

        public void AddCustomModel(ChatEditorProviderType providerType_, ChatModelInfo modelInfo_)
        {
            _chatModelPresets.AddCustomChatModel(providerType_, modelInfo_);
            _chatModelPresets.SaveToStorage(_storage);
        }

        public void RemoveCustomModel(ChatEditorProviderType providerType_, string modelId_)
        {
            _chatModelPresets.RemoveCustomChatModel(providerType_, modelId_);
            _chatModelPresets.SaveToStorage(_storage);
        }

        public string GetDefaultChatModel(ChatEditorProviderType providerType_)
        {
            return _chatModelPresets.GetDefaultChatModel(providerType_);
        }

        public string GetModelsUrl(ChatEditorProviderType providerType_)
        {
            return _chatModelPresets.GetModelsUrl(providerType_);
        }

        private List<string> GetStoredSelectedModelIds(ChatEditorProviderType providerType_)
        {
            List<string> modelIds = new List<string>();
            if (_storage == null)
            {
                return modelIds;
            }

            string listKey = EditorDataStorageKeys.GetSelectedChatModelListKey(providerType_);
            if (string.IsNullOrEmpty(listKey))
            {
                return modelIds;
            }

            string serialized = _storage.GetString(listKey);
            if (string.IsNullOrEmpty(serialized))
                return modelIds;

            return ParseModelIdList(serialized);
        }

        private string GetStoredSelectedModelId(ChatEditorProviderType providerType_)
        {
            if (_storage == null)
                return string.Empty;

            string key = EditorDataStorageKeys.GetSelectedChatModelKey(providerType_);
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return _storage.GetString(key);
        }

        private void SaveSelectedModelIds(ChatEditorProviderType providerType_, List<string> modelIds_)
        {
            if (_storage == null)
            {
                Debug.LogWarning($"[EditorProviderManagerChat] SaveSelectedModelIds: _storage is null for {providerType_}");
                return;
            }

            string listKey = EditorDataStorageKeys.GetSelectedChatModelListKey(providerType_);
            if (string.IsNullOrEmpty(listKey))
            {
                Debug.LogWarning($"[EditorProviderManagerChat] SaveSelectedModelIds: listKey is empty for {providerType_}");
                return;
            }

            string serialized = SerializeModelIdList(modelIds_);
            _storage.SetString(listKey, serialized);
            _storage.Save();
        }

        private void SaveSelectedPrimaryModelId(ChatEditorProviderType providerType_, string modelId_)
        {
            if (_storage == null)
                return;

            string key = EditorDataStorageKeys.GetSelectedChatModelKey(providerType_);
            if (string.IsNullOrEmpty(key))
                return;

            _storage.SetString(key, modelId_ ?? string.Empty);
            _storage.Save();

            OnSelectedModelChanged?.Invoke(providerType_);
        }

        private List<string> NormalizeModelIdList(ChatEditorProviderType providerType_, List<string> modelIds_)
        {
            List<string> normalized = new List<string>();
            if (modelIds_ == null)
                return normalized;

            HashSet<string> availableModelIds = new HashSet<string>();
            List<ChatModelInfo> availableModels = _chatModelPresets.GetChatModelInfos(providerType_);
            foreach (ChatModelInfo modelInfo in availableModels)
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
