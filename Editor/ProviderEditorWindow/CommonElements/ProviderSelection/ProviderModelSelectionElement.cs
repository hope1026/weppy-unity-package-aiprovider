using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Chat.Editor
{
    public class ProviderModelSelectionElement<TProviderType, TModelInfo> : VisualElement
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        public event Action OnAuthChanged;

        private const string SELECTED_MODELS_CHIPS_NAME = "selected-models-chips";

        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;
        private readonly VisualElement _root;
        private ProviderModelSelectionPopupView<TProviderType, TModelInfo> _popupView;

        private readonly Dictionary<TProviderType, List<string>> _selectedModelIdsByProvider =
            new Dictionary<TProviderType, List<string>>();

        private readonly Dictionary<TProviderType, string> _primaryModelIdByProvider =
            new Dictionary<TProviderType, string>();

        private readonly Dictionary<TProviderType, HashSet<string>> _disabledModelIdsByProvider =
            new Dictionary<TProviderType, HashSet<string>>();

        public EditorDataStorage Storage { get; private set; }
        public string CurrentLanguageCode { get; private set; }

        public Button ModelSelectionButton { get; private set; }
        public VisualElement SelectedModelsChipsContainer { get; private set; }
        public VisualElement ModelSelectionPopup { get; internal set; }
        public VisualElement PopupProviderList { get; internal set; }
        public VisualElement PopupOverlay { get; internal set; }

        public Dictionary<TProviderType, Toggle> ProviderToggles { get; } = new Dictionary<TProviderType, Toggle>();
        public Dictionary<TProviderType, IntegerField> ProviderPriorities { get; } = new Dictionary<TProviderType, IntegerField>();
        public Dictionary<TProviderType, DropdownField> ProviderModels { get; } = new Dictionary<TProviderType, DropdownField>();

        public Dictionary<TProviderType, Dictionary<string, string>> ModelDisplayToId { get; } =
            new Dictionary<TProviderType, Dictionary<string, string>>();

        public DragDropReorderController<TProviderType> DragDropController { get; private set; }
        public DragDropReorderController<TProviderType> PopupDragDropController { get; internal set; }

        internal ProviderModelSelectionConfig<TProviderType, TModelInfo> Config => _config;
        internal VisualElement RootElement => _root;

        public EditorProviderManagerInterface<TProviderType, TModelInfo> Manager { get; private set; }

        public ProviderModelSelectionElement(
            VisualElement root_,
            ProviderModelSelectionConfig<TProviderType, TModelInfo> config_,
            EditorProviderManagerInterface<TProviderType, TModelInfo> manager_ = null)
        {
            _root = root_ ?? this;
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
            Manager = manager_;
        }

        public void Initialize(EditorDataStorage storage_, string languageCode_)
        {
            Storage = storage_;
            CurrentLanguageCode = languageCode_;
        }

        public void SetModelState(EditorProviderManagerInterface<TProviderType, TModelInfo> manager_)
        {
            Manager = manager_;
        }

        public void SetupUI(string modelSelectionButtonName_)
        {
            ModelSelectionButton = _root.Q<Button>(modelSelectionButtonName_);
            SelectedModelsChipsContainer = _root.Q<VisualElement>(SELECTED_MODELS_CHIPS_NAME);

            if (ModelSelectionButton != null)
            {
                ModelSelectionButton.clicked += ToggleModelSelectionPopup;
            }
        }

        public void UpdateLanguage(string languageCode_)
        {
            CurrentLanguageCode = languageCode_;
            UpdateSelectedModelChips();
            _popupView?.RefreshLanguage(languageCode_);
        }

        public void OnHide()
        {
            _popupView?.Close();
        }

        public void InitializeDragDropController(
            VisualElement listRoot_,
            Func<TProviderType, bool> canReorder_,
            Action onOrderChanged_,
            Action<TProviderType> showApiKeyWarning_)
        {
            DragDropController = new DragDropReorderController<TProviderType>(
                listRoot_,
                canReorder_,
                onOrderChanged_,
                showApiKeyWarning_);
        }

        public List<TProviderType> LoadProviderOrder()
        {
            List<TProviderType> defaultOrder = new List<TProviderType>();
            if (Manager != null)
            {
                List<TProviderType> providers = Manager.GetProviders();
                foreach (TProviderType providerType in providers)
                {
                    if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                        continue;

                    defaultOrder.Add(providerType);
                }
            }
            else
            {
                TProviderType[] values = (TProviderType[])Enum.GetValues(typeof(TProviderType));
                foreach (TProviderType providerType in values)
                {
                    if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                        continue;

                    defaultOrder.Add(providerType);
                }
            }

            if (Storage == null)
                return defaultOrder;

            string savedOrder = Storage.GetString(_config.ProviderOrderStorageKey);
            if (string.IsNullOrEmpty(savedOrder))
                return defaultOrder;

            string[] orderParts = savedOrder.Split(',');
            List<TProviderType> loadedOrder = new List<TProviderType>();

            foreach (string part in orderParts)
            {
                if (Enum.TryParse(part.Trim(), out TProviderType providerType) && defaultOrder.Contains(providerType))
                {
                    loadedOrder.Add(providerType);
                }
            }

            foreach (TProviderType providerType in defaultOrder)
            {
                if (!loadedOrder.Contains(providerType))
                {
                    loadedOrder.Add(providerType);
                }
            }

            return loadedOrder;
        }

        public void SaveProviderOrder()
        {
            if (Storage == null || DragDropController == null)
                return;

            string orderString = string.Join(",", DragDropController.ItemOrder);
            Storage.SetString(_config.ProviderOrderStorageKey, orderString);
            Storage.Save();
        }

        public void UpdatePrioritiesFromOrder()
        {
            if (DragDropController == null)
                return;

            IReadOnlyList<TProviderType> providerOrder = DragDropController.ItemOrder;
            for (int i = 0; i < providerOrder.Count; i++)
            {
                TProviderType providerType = providerOrder[i];
                int priority = DragDropController.GetPriorityForIndex(i);

                if (ProviderPriorities.TryGetValue(providerType, out IntegerField priorityField))
                {
                    priorityField.SetValueWithoutNotify(priority);
                }

                _config.SetProviderPriority?.Invoke(providerType, priority);
            }
        }

        public List<TProviderType> SortProvidersByEnabledState(List<TProviderType> providerOrder_)
        {
            List<(TProviderType providerType, int originalIndex, bool isEnabled, bool hasKey)> providers =
                new List<(TProviderType, int, bool, bool)>();

            for (int i = 0; i < providerOrder_.Count; i++)
            {
                TProviderType providerType = providerOrder_[i];
                bool hasKey = _config.HasApiKey != null && _config.HasApiKey(providerType);
                bool isEnabled = hasKey;
                providers.Add((providerType, i, isEnabled, hasKey));
            }

            providers.Sort((a_, b_) =>
            {
                if (a_.isEnabled != b_.isEnabled)
                    return b_.isEnabled.CompareTo(a_.isEnabled);
                return a_.originalIndex.CompareTo(b_.originalIndex);
            });

            List<TProviderType> sortedOrder = new List<TProviderType>();
            foreach ((TProviderType providerType, int originalIndex, bool isEnabled, bool hasKey) item in providers)
            {
                sortedOrder.Add(item.providerType);
            }

            return sortedOrder;
        }

        public void UpdateSelectedModelChips()
        {
            if (SelectedModelsChipsContainer == null)
                return;

            SelectedModelsChipsContainer.Clear();

            if (DragDropController == null)
                return;

            IReadOnlyList<TProviderType> providerOrder = DragDropController.ItemOrder;

            List<(TProviderType providerType, int originalIndex, bool hasApiKey, HashSet<string> allModelIds)> validProviders =
                new List<(TProviderType, int, bool, HashSet<string>)>();

            for (int i = 0; i < providerOrder.Count; i++)
            {
                TProviderType providerType = providerOrder[i];

                bool hasKey = _config.HasApiKey != null && _config.HasApiKey(providerType);
                if (!hasKey)
                    continue;

                List<string> selectedModelIds = GetSelectedModelIdsInternal(providerType);
                HashSet<string> disabledModelIds = GetDisabledModelIds(providerType);

                HashSet<string> allModelIds = new HashSet<string>(selectedModelIds);
                allModelIds.UnionWith(disabledModelIds);

                if (allModelIds.Count == 0)
                    continue;

                validProviders.Add((providerType, i, hasKey, allModelIds));
            }

            validProviders.Sort((a_, b_) =>
            {
                if (a_.hasApiKey != b_.hasApiKey)
                    return b_.hasApiKey.CompareTo(a_.hasApiKey);
                return a_.originalIndex.CompareTo(b_.originalIndex);
            });

            int selectedCount = 0;

            foreach ((TProviderType providerType, int originalIndex, bool hasApiKey, HashSet<string> allModelIds) item in validProviders)
            {
                TProviderType providerType = item.providerType;
                bool hasApiKey = item.hasApiKey;

                int priority = DragDropController.GetPriorityForIndex(item.originalIndex);
                HashSet<string> disabledModelIds = GetDisabledModelIds(providerType);

                foreach (string modelId in item.allModelIds)
                {
                    bool isModelDisabled = disabledModelIds.Contains(modelId);

                    if (!isModelDisabled)
                    {
                        selectedCount++;
                    }

                    TModelInfo modelInfo = _config.GetModelInfo?.Invoke(providerType, modelId);
                    string displayName = _config.GetModelDisplayName?.Invoke(modelInfo, modelId) ?? modelId;
                    string chipLabelText = $"{providerType} - {displayName}";

                    VisualElement chip = new VisualElement();
                    chip.AddToClassList("model-chip");

                    if (isModelDisabled)
                    {
                        chip.AddToClassList("model-chip--disabled");
                    }

                    string tooltipText = "";
                    if (modelInfo != null)
                    {
                        string description = _config.GetModelDescription?.Invoke(modelInfo, CurrentLanguageCode);
                        if (!string.IsNullOrEmpty(description))
                        {
                            tooltipText = description;
                        }
                    }

                    string toggleHint = LocalizationManager.Get(LocalizationKeys.COMMON_MODEL_SELECTION);
                    chip.tooltip = string.IsNullOrEmpty(tooltipText) ? toggleHint : $"{tooltipText}\n{toggleHint}";

                    TProviderType capturedProviderType = providerType;
                    string capturedModelId = modelId;
                    chip.RegisterCallback<ClickEvent>(evt_ =>
                    {
                        ToggleModelActiveState(capturedProviderType, capturedModelId);
                        UpdateSelectedModelChips();
                        evt_.StopPropagation();
                    });

                    Label nameLabel = new Label(chipLabelText);
                    nameLabel.AddToClassList("model-chip-label");
                    chip.Add(nameLabel);

                    Label priorityLabel = new Label($"#{priority}");
                    priorityLabel.AddToClassList("model-chip-priority");
                    chip.Add(priorityLabel);

                    SelectedModelsChipsContainer.Add(chip);
                }
            }

            UpdateSelectionButtonText(selectedCount);
        }

        public void Dispose()
        {
            if (ModelSelectionButton != null)
            {
                ModelSelectionButton.clicked -= ToggleModelSelectionPopup;
            }

            _popupView?.Close();
        }

        public int GetModelPriority(TProviderType providerType_, string modelId_)
        {
            if (Storage == null)
                return 0;

            string key = EditorDataStorageKeys.GetModelPriorityKey(providerType_.ToString(), modelId_);
            if (string.IsNullOrEmpty(key))
                return 0;

            return Storage.GetInt(key, 0);
        }

        public void SetModelPriority(TProviderType providerType_, string modelId_, int priority_)
        {
            if (Storage == null)
                return;

            string key = EditorDataStorageKeys.GetModelPriorityKey(providerType_.ToString(), modelId_);
            if (string.IsNullOrEmpty(key))
                return;

            Storage.SetInt(key, priority_);
            Storage.Save();
        }

        public string GetModelIdFromDisplay(TProviderType providerType_, string displayText_)
        {
            if (displayText_ == null)
            {
                return null;
            }

            if (ModelDisplayToId.TryGetValue(providerType_, out Dictionary<string, string> mapping))
            {
                if (mapping.TryGetValue(displayText_, out string modelId))
                {
                    return modelId;
                }
            }

            return displayText_;
        }

        private void UpdateSelectionButtonText(int selectedCount_)
        {
            if (ModelSelectionButton == null)
                return;

            ModelSelectionButton.text = _config.GetSelectionButtonText != null
                ? _config.GetSelectionButtonText(selectedCount_)
                : $"({selectedCount_})";
        }

        private void ToggleModelSelectionPopup()
        {
            EnsurePopupView();
            _popupView.Toggle();
        }

        private void ShowModelSelectionPopup()
        {
            EnsurePopupView();
            _popupView.Show();
        }

        private void SetActiveProviderInPopup(TProviderType providerType_, bool rebuildModels_)
        {
            if (_popupView == null)
            {
                return;
            }

            _popupView.SetActiveProvider(providerType_, rebuildModels_);
        }

        public void ShowApiKeyInputForFirstProviderWithoutKey()
        {
            EnsurePopupView();

            // Find the first provider without an API key
            TProviderType firstProviderWithoutKey = default;
            bool found = false;

            if (DragDropController != null && DragDropController.ItemOrder != null)
            {
                foreach (TProviderType providerType in DragDropController.ItemOrder)
                {
                    if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                        continue;

                    bool hasKey = _config.HasApiKey != null && _config.HasApiKey(providerType);
                    if (!hasKey)
                    {
                        firstProviderWithoutKey = providerType;
                        found = true;
                        break;
                    }
                }
            }

            // Show popup and set active provider
            _popupView.Show();

            if (found)
            {
                _popupView.SetActiveProvider(firstProviderWithoutKey, true);
            }

            // Highlight API key section
            _popupView.ShowApiKeyInput();
        }

        private void EnsurePopupView()
        {
            if (_popupView != null)
            {
                return;
            }

            _popupView = new ProviderModelSelectionPopupView<TProviderType, TModelInfo>(this, _config, Storage);
        }

        internal void NotifyAuthChanged()
        {
            OnAuthChanged?.Invoke();
        }

        internal List<string> GetSelectedModelIdsInternal(TProviderType providerType_)
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

        internal void SetSelectedModelIdsInternal(TProviderType providerType_, List<string> modelIds_)
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

        internal string GetPrimaryModelId(TProviderType providerType_, List<string> modelIds_)
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

        internal List<string> NormalizeModelIdList(TProviderType providerType_, List<string> modelIds_)
        {
            List<string> normalized = new List<string>();
            if (modelIds_ == null)
            {
                return normalized;
            }

            HashSet<string> enabledModelIds = new HashSet<string>();
            List<TModelInfo> enabledModels = _config.GetAllModelInfos != null ? _config.GetAllModelInfos(providerType_) : new List<TModelInfo>();

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

        internal void SetPrimaryModelId(TProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
            {
                return;
            }

            _primaryModelIdByProvider[providerType_] = modelId_;
            _config.SaveSelectedModelId?.Invoke(providerType_, modelId_);
            _config.NotifyModelChanged?.Invoke(providerType_, modelId_);
        }

        internal VisualElement GetRootVisualContainer()
        {
            VisualElement current = _root;
            while (current != null)
            {
                if (!string.IsNullOrEmpty(_config.RootContainerClassName) &&
                    current.ClassListContains(_config.RootContainerClassName))
                {
                    return current;
                }

                if (!string.IsNullOrEmpty(_config.RootContainerClassName))
                {
                    VisualElement foundContainer = current.Q(className: _config.RootContainerClassName);
                    if (foundContainer != null)
                    {
                        return foundContainer;
                    }
                }

                current = current.parent;
            }

            if (_root.panel?.visualTree != null)
            {
                if (!string.IsNullOrEmpty(_config.RootContainerClassName))
                {
                    VisualElement containerFromPanel = _root.panel.visualTree.Q(className: _config.RootContainerClassName);
                    if (containerFromPanel != null)
                    {
                        return containerFromPanel;
                    }
                }

                return _root.panel.visualTree;
            }

            return null;
        }

        internal string BuildModelSearchText(string modelId_, string displayName_, string description_, TModelInfo modelInfo_)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(modelId_))
                parts.Add(modelId_);
            if (!string.IsNullOrEmpty(displayName_))
                parts.Add(displayName_);
            if (!string.IsNullOrEmpty(description_))
                parts.Add(description_);

            string displayText = _config.GetModelDisplayText != null
                ? _config.GetModelDisplayText(modelInfo_, CurrentLanguageCode)
                : string.Empty;
            if (!string.IsNullOrEmpty(displayText))
                parts.Add(displayText);

            return string.Join(" ", parts);
        }

        internal bool MatchesSearch(string haystack_, string needle_)
        {
            if (string.IsNullOrEmpty(needle_))
                return true;

            if (string.IsNullOrEmpty(haystack_))
                return false;

            return haystack_.IndexOf(needle_, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private HashSet<string> GetDisabledModelIds(TProviderType providerType_)
        {
            if (!_disabledModelIdsByProvider.TryGetValue(providerType_, out HashSet<string> disabledIds))
            {
                disabledIds = new HashSet<string>();
                _disabledModelIdsByProvider[providerType_] = disabledIds;
            }

            return disabledIds;
        }

        private void ToggleModelActiveState(TProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            HashSet<string> disabledIds = GetDisabledModelIds(providerType_);
            List<string> selectedIds = GetSelectedModelIdsInternal(providerType_);

            if (disabledIds.Contains(modelId_))
            {
                disabledIds.Remove(modelId_);
                if (!selectedIds.Contains(modelId_))
                {
                    selectedIds.Add(modelId_);
                    SetSelectedModelIdsInternal(providerType_, selectedIds);
                }
            }
            else
            {
                disabledIds.Add(modelId_);
                if (selectedIds.Contains(modelId_))
                {
                    selectedIds.Remove(modelId_);
                    SetSelectedModelIdsInternal(providerType_, selectedIds);
                }
            }
        }
    }
}
