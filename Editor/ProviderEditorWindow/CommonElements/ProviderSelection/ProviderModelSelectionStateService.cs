using System;
using System.Collections.Generic;

namespace Weppy.AIProvider.Editor
{
    public class ProviderModelSelectionStateService<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;

        private readonly Dictionary<TProviderType, List<string>> _selectedModelIdsByProvider =
            new Dictionary<TProviderType, List<string>>();

        private readonly Dictionary<TProviderType, string> _primaryModelIdByProvider =
            new Dictionary<TProviderType, string>();

        private readonly Dictionary<TProviderType, HashSet<string>> _disabledModelIdsByProvider =
            new Dictionary<TProviderType, HashSet<string>>();

        public ProviderModelSelectionStateService(ProviderModelSelectionConfig<TProviderType, TModelInfo> config_)
        {
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
        }

        public List<string> GetSelectedModelIds(TProviderType providerType_)
        {
            if (_selectedModelIdsByProvider.TryGetValue(providerType_, out List<string> cachedModelIds))
            {
                List<string> normalizedCached = NormalizeModelIdList(providerType_, cachedModelIds);
                _selectedModelIdsByProvider[providerType_] = normalizedCached;
                return new List<string>(normalizedCached);
            }

            IReadOnlyList<string> modelIdsReadOnly = _config.GetSelectedModelIds != null
                ? _config.GetSelectedModelIds(providerType_)
                : new List<string>();

            List<string> modelIds = modelIdsReadOnly != null
                ? new List<string>(modelIdsReadOnly)
                : new List<string>();

            if (modelIds.Count == 0)
            {
                string fallbackId = _config.GetPrimarySelectedModelId?.Invoke(providerType_);
                if (!string.IsNullOrEmpty(fallbackId))
                {
                    modelIds.Add(fallbackId);
                }
            }

            List<string> normalized = NormalizeModelIdList(providerType_, modelIds);
            _selectedModelIdsByProvider[providerType_] = normalized;

            string primaryId = GetPrimaryModelId(providerType_, normalized);
            if (!string.IsNullOrEmpty(primaryId))
            {
                _primaryModelIdByProvider[providerType_] = primaryId;
            }

            return new List<string>(normalized);
        }

        public void SetSelectedModelIds(TProviderType providerType_, List<string> modelIds_)
        {
            List<string> normalized = NormalizeModelIdList(providerType_, modelIds_);
            _selectedModelIdsByProvider[providerType_] = normalized;

            _config.SaveSelectedModelIds?.Invoke(providerType_, new List<string>(normalized));

            if (normalized.Count == 0)
            {
                _primaryModelIdByProvider.Remove(providerType_);
                _config.SaveSelectedModelId?.Invoke(providerType_, string.Empty);
                _config.NotifyModelChanged?.Invoke(providerType_, string.Empty);
                return;
            }

            string primaryId = GetPrimaryModelId(providerType_, normalized);
            _primaryModelIdByProvider[providerType_] = primaryId;

            _config.SaveSelectedModelId?.Invoke(providerType_, primaryId);
            _config.NotifyModelChanged?.Invoke(providerType_, primaryId);
        }

        public string GetPrimaryModelId(TProviderType providerType_, List<string> modelIds_)
        {
            if (_primaryModelIdByProvider.TryGetValue(providerType_, out string primaryId) &&
                !string.IsNullOrEmpty(primaryId) &&
                modelIds_.Contains(primaryId))
            {
                return primaryId;
            }

            string fallbackId = _config.GetPrimarySelectedModelId?.Invoke(providerType_);
            if (!string.IsNullOrEmpty(fallbackId) && modelIds_.Contains(fallbackId))
            {
                return fallbackId;
            }

            return modelIds_.Count > 0 ? modelIds_[0] : string.Empty;
        }

        public List<string> NormalizeModelIdList(TProviderType providerType_, List<string> modelIds_)
        {
            List<string> normalized = new List<string>();
            if (modelIds_ == null)
            {
                return normalized;
            }

            HashSet<string> enabledModelIds = new HashSet<string>();
            List<TModelInfo> enabledModels = _config.GetAllModelInfos != null
                ? _config.GetAllModelInfos(providerType_)
                : new List<TModelInfo>();

            foreach (TModelInfo modelInfo in enabledModels)
            {
                string modelId = _config.GetModelIdFromInfo != null ? _config.GetModelIdFromInfo(modelInfo) : string.Empty;
                if (!string.IsNullOrEmpty(modelId))
                {
                    enabledModelIds.Add(modelId);
                }
            }

            foreach (string modelId in modelIds_)
            {
                if (string.IsNullOrEmpty(modelId))
                    continue;

                if (enabledModelIds.Count > 0 && !enabledModelIds.Contains(modelId))
                    continue;

                if (!normalized.Contains(modelId))
                {
                    normalized.Add(modelId);
                }
            }

            return normalized;
        }

        public void SetPrimaryModelId(TProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            _primaryModelIdByProvider[providerType_] = modelId_;
            _config.SaveSelectedModelId?.Invoke(providerType_, modelId_);
            _config.NotifyModelChanged?.Invoke(providerType_, modelId_);
        }

        public HashSet<string> GetDisabledModelIds(TProviderType providerType_)
        {
            if (!_disabledModelIdsByProvider.TryGetValue(providerType_, out HashSet<string> disabledIds))
            {
                disabledIds = new HashSet<string>();
                _disabledModelIdsByProvider[providerType_] = disabledIds;
            }

            return disabledIds;
        }

        public void ToggleModelActiveState(TProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            HashSet<string> disabledIds = GetDisabledModelIds(providerType_);
            List<string> selectedIds = GetSelectedModelIds(providerType_);

            if (disabledIds.Contains(modelId_))
            {
                disabledIds.Remove(modelId_);
                if (!selectedIds.Contains(modelId_))
                {
                    selectedIds.Add(modelId_);
                    SetSelectedModelIds(providerType_, selectedIds);
                }
            }
            else
            {
                disabledIds.Add(modelId_);
                if (selectedIds.Contains(modelId_))
                {
                    selectedIds.Remove(modelId_);
                    SetSelectedModelIds(providerType_, selectedIds);
                }
            }
        }
    }
}
