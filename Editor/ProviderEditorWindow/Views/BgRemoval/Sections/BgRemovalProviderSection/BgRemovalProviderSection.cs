using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class BgRemovalProviderSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "BgRemoval/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "BgRemovalProviderSection/BgRemovalProviderSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "BgRemovalProviderSection/BgRemovalProviderSection.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnNavigateToSettingsRequested;
        public event Action OnProvidersChanged;

        private BgRemovalProviderManager _bgRemovalManager;
        private EditorDataStorage _storage;
        private EditorProviderManagerBgRemoval _editorProviderManager;
        private ProviderModelSelectionElement<BgRemovalProviderType, BgRemovalModelInfo> _providerSelection;
        private VisualElement _providerList;

        public BgRemovalProviderSection()
        {
            LoadLayout();
            LoadStyles();
            SetupProviderSelection();
            SetupUI();
        }

        private void LoadLayout()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);
            if (visualTree != null)
            {
                visualTree.CloneTree(this);
            }
        }

        private void LoadStyles()
        {
            StyleSheet sectionStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (sectionStyles != null)
            {
                styleSheets.Add(sectionStyles);
            }
        }

        private void SetupProviderSelection()
        {
            ProviderModelSelectionConfig<BgRemovalProviderType, BgRemovalModelInfo> config =
                new ProviderModelSelectionConfig<BgRemovalProviderType, BgRemovalModelInfo>
                {
                    RootContainerClassName = "bgremoval-view-container",
                    ProviderOrderStorageKey = "bgremoval_provider_order",
                    SectionUssPath = USS_PATH,
                    IsNoneProvider = providerType_ => providerType_ == BgRemovalProviderType.NONE,
                    HasProviderAuth = providerType_ => !string.IsNullOrEmpty(EditorDataStorageKeys.GetApiKey(_storage, providerType_)),
                    IsProviderEnabled = GetProviderEnabledStateInternal,
                    GetSelectionButtonText = selectedCount_ =>
                        LocalizationManager.Get(LocalizationKeys.COMMON_MODEL_SELECTION) + $" ({selectedCount_})",
                    GetAllModelInfos = GetAllModelInfos,
                    GetPrimarySelectedModelId = GetPrimarySelectedModel,
                    GetSelectedModelIds = GetSelectedModelIds,
                    SaveSelectedModelId = SaveSelectedModelId,
                    SaveSelectedModelIds = SaveSelectedModelIds,
                    GetModelInfo = GetModelInfo,
                    GetModelDisplayText = CreateModelDisplayText,
                    GetModelDisplayName = (modelInfo_, fallback_) => modelInfo_ != null ? modelInfo_.DisplayName : fallback_,
                    GetModelDescription = (modelInfo_, languageCode_) => modelInfo_.GetDescription(languageCode_),
                    GetModelPriceDisplay = modelInfo_ => modelInfo_ != null ? modelInfo_.GetPriceDisplay() : string.Empty,
                    GetNoModelsEnabledLabelText = _ => LocalizationManager.Get(LocalizationKeys.BGREMOVAL_NO_MODELS_ENABLED_TITLE),
                    GetModelsUrl = providerType_ =>
                    {
                        EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
                        return manager != null ? manager.GetModelsUrl(providerType_) : string.Empty;
                    },
                    NotifyProviderOrderChanged = () => OnProvidersChanged?.Invoke(),
                    NotifyProviderEnabledChanged = (providerType_, enabled_) =>
                    {
                        if (_providerSelection.ProviderToggles.TryGetValue(providerType_, out Toggle toggle) && toggle != null)
                        {
                            toggle.SetValueWithoutNotify(enabled_);
                        }

                        _bgRemovalManager?.SetProviderEnabled(providerType_, enabled_);
                        UpdateSelectedModelChips();
                        OnProvidersChanged?.Invoke();
                    },
                    NotifyModelChanged = (_, _) => OnProvidersChanged?.Invoke(),
                    GetModelIdFromInfo = modelInfo_ => modelInfo_.Id,
                    GetContextWindowSize = modelInfo_ => modelInfo_.ContextWindowSize,
                    GetMaxOutputTokens = modelInfo_ => modelInfo_.MaxOutputTokens,
                    GetModelPrice = modelInfo_ => modelInfo_?.PricePerImage ?? 0,
                    AddCustomModel = (providerType_, modelInfo_) =>
                    {
                        AddCustomModel(providerType_, modelInfo_);
                    },
                    CreateCustomModelInfo = providerType_ => new BgRemovalModelInfo(),
                    RemoveCustomModel = (providerType_, modelId_) =>
                    {
                        RemoveCustomModel(providerType_, modelId_);
                    }
                };

            _providerSelection = new ProviderModelSelectionElement<BgRemovalProviderType, BgRemovalModelInfo>(
                this,
                config,
                _editorProviderManager);
            _providerSelection.SetupUI("bgremoval-provider-selection-button");
        }

        public void Initialize(
            EditorDataStorage storage_,
            BgRemovalProviderManager manager_,
            EditorProviderManagerBgRemoval editorProviderManager_,
            string languageCode_)
        {
            _storage = storage_;
            _bgRemovalManager = manager_;
            _editorProviderManager = editorProviderManager_;
            _providerSelection.Initialize(storage_, languageCode_);
            _providerSelection.SetModelState(_editorProviderManager);
            PopulateProviderList();
            UpdateSectionHeaders();
        }

        public void SetManager(BgRemovalProviderManager manager_)
        {
            _bgRemovalManager = manager_;
            _providerSelection.UpdatePrioritiesFromOrder();
            UpdateSelectedModelChips();
        }

        private void SetupUI()
        {
            _providerList = new VisualElement();
            _providerList.name = "bgremoval-provider-list";
            _providerList.AddToClassList("bgremoval-provider-list");

            VisualElement headerRow = this.Q<VisualElement>("providers-header-row");
            if (headerRow != null && headerRow.parent != null)
            {
                headerRow.parent.Add(_providerList);
            }
            else
            {
                Add(_providerList);
            }

            UpdateSelectedModelChips();
        }

        public void UpdateLanguage(string languageCode_)
        {
            _providerSelection.UpdateLanguage(languageCode_);
            UpdateSectionHeaders();
        }

        private void UpdateSectionHeaders()
        {
            if (_providerSelection.ModelSelectionButton != null)
            {
                _providerSelection.ModelSelectionButton.tooltip =
                    LocalizationManager.Get(LocalizationKeys.PROVIDER_PRIORITY_DESCRIPTION);
            }

            UpdateSelectedModelChips();
        }

        public void RefreshProviders()
        {
            PopulateProviderList();
        }

        public bool HasEnabledProvider()
        {
            foreach (KeyValuePair<BgRemovalProviderType, Toggle> kvp in _providerSelection.ProviderToggles)
            {
                if (kvp.Value != null && kvp.Value.value)
                {
                    if (!string.IsNullOrEmpty(EditorDataStorageKeys.GetApiKey(_storage, kvp.Key)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public string GetPrimarySelectedModel(BgRemovalProviderType providerType_)
        {
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            return manager != null ? manager.GetPrimarySelectedModelId(providerType_) : string.Empty;
        }

        public List<BgRemovalRequestProviderTarget> BuildProviderTargets()
        {
            List<BgRemovalRequestProviderTarget> targets = new List<BgRemovalRequestProviderTarget>();
            if (_providerSelection == null)
                return targets;

            IReadOnlyList<BgRemovalProviderType> providerOrder = _providerSelection.DragDropController != null
                ? _providerSelection.DragDropController.ItemOrder
                : _providerSelection.LoadProviderOrder();

            for (int i = 0; i < providerOrder.Count; i++)
            {
                BgRemovalProviderType providerType = providerOrder[i];
                string apiKey = EditorDataStorageKeys.GetApiKey(_storage, providerType);
                if (string.IsNullOrEmpty(apiKey))
                    continue;

                if (!GetProviderEnabledStateInternal(providerType))
                    continue;

                IReadOnlyList<string> selectedModelIds = GetSelectedModelIds(providerType);
                if (selectedModelIds == null || selectedModelIds.Count == 0)
                    continue;

                int priority = _providerSelection.DragDropController != null
                    ? _providerSelection.DragDropController.GetPriorityForIndex(i)
                    : 100 - (i * 10);

                targets.Add(new BgRemovalRequestProviderTarget
                {
                    ProviderType = providerType,
                    Model = GetPrimarySelectedModel(providerType),
                    Priority = priority
                });
            }

            return targets;
        }

        public void ShowApiKeyInput()
        {
            _providerSelection?.ShowApiKeyInputForFirstProviderWithoutKey();
        }

        private IReadOnlyList<string> GetSelectedModelIds(BgRemovalProviderType providerType_)
        {
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            return manager != null ? manager.GetSelectedModelIds(providerType_) : new List<string>();
        }

        private void SaveSelectedModelIds(BgRemovalProviderType providerType_, List<string> modelIds_)
        {
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            manager?.SetSelectedModelIds(providerType_, modelIds_);
        }

        private void PopulateProviderList()
        {
            if (_providerList == null)
                return;

            _providerList.Clear();
            _providerList.style.display = DisplayStyle.None;
            _providerSelection.ProviderToggles.Clear();
            _providerSelection.ProviderPriorities.Clear();
            _providerSelection.ProviderModels.Clear();
            _providerSelection.ModelDisplayToId.Clear();

            _providerSelection.InitializeDragDropController(
                _providerList,
                providerType => !string.IsNullOrEmpty(EditorDataStorageKeys.GetApiKey(_storage, providerType)),
                OnProviderOrderChanged,
                ShowApiKeyRequiredWarningInternal
            );

            List<BgRemovalProviderType> providerOrder = _providerSelection.LoadProviderOrder();
            _providerSelection.DragDropController.Initialize(providerOrder);

            foreach (BgRemovalProviderType providerType in _providerSelection.DragDropController.ItemOrder)
            {
                VisualElement entry = CreateProviderEntry(providerType);
                _providerSelection.DragDropController.SetupEntry(entry, providerType);
                _providerList.Add(entry);
            }

            _providerSelection.UpdatePrioritiesFromOrder();
            UpdateSelectedModelChips();
            OnProvidersChanged?.Invoke();
        }

        private void OnProviderOrderChanged()
        {
            _providerSelection.SaveProviderOrder();
            _providerSelection.UpdatePrioritiesFromOrder();
            UpdateSelectedModelChips();
            OnProvidersChanged?.Invoke();
        }

        private VisualElement CreateProviderEntry(BgRemovalProviderType providerType_)
        {
            VisualElement entry = new VisualElement();
            entry.AddToClassList("provider-entry");
            entry.AddToClassList("provider-entry-two-column");

            bool hasKey = !string.IsNullOrEmpty(EditorDataStorageKeys.GetApiKey(_storage, providerType_));

            VisualElement leftColumn = new VisualElement();
            leftColumn.AddToClassList("provider-left-column");

            Toggle toggle = new Toggle();
            toggle.AddToClassList("provider-enabled-toggle");
            toggle.value = hasKey;
            toggle.SetEnabled(hasKey);
            toggle.RegisterValueChangedCallback(evt =>
            {
                _bgRemovalManager?.SetProviderEnabled(providerType_, evt.newValue);
                UpdateSelectedModelChips();
                OnProvidersChanged?.Invoke();
            });
            _providerSelection.ProviderToggles[providerType_] = toggle;

            VisualElement toggleWrapper = new VisualElement();
            toggleWrapper.AddToClassList("reorder-toggle-wrapper");
            toggleWrapper.Add(toggle);

            if (!hasKey)
            {
                toggleWrapper.RegisterCallback<ClickEvent>(evt_ =>
                {
                    ShowApiKeyRequiredWarningInternal(providerType_);
                    evt_.StopPropagation();
                });
            }

            Label nameLabel = new Label(providerType_.ToString());
            nameLabel.AddToClassList("provider-name");

            Label priorityLabel = new Label(LocalizationManager.Get(LocalizationKeys.COMMON_PRIORITY) + ":");
            priorityLabel.AddToClassList("provider-priority-label");

            IntegerField priorityField = new IntegerField();
            priorityField.AddToClassList("provider-priority");
            priorityField.AddToClassList("provider-priority-readonly");
            priorityField.value = GetDefaultPriority(providerType_);
            priorityField.isReadOnly = true;
            _providerSelection.ProviderPriorities[providerType_] = priorityField;

            Label statusLabel = new Label(hasKey
                ? LocalizationManager.Get(LocalizationKeys.SETTINGS_API_KEY_SET)
                : LocalizationManager.Get(LocalizationKeys.SETTINGS_NO_API_KEY));
            statusLabel.AddToClassList("provider-status");
            statusLabel.AddToClassList(hasKey ? "key-status-set" : "key-status-missing");

            if (!hasKey)
            {
                statusLabel.AddToClassList("provider-status-clickable");
                statusLabel.RegisterCallback<ClickEvent>(_ => { ShowApiKeyRequiredWarningInternal(providerType_); });
            }

            leftColumn.Add(toggleWrapper);
            leftColumn.Add(nameLabel);
            leftColumn.Add(priorityLabel);
            leftColumn.Add(priorityField);
            leftColumn.Add(statusLabel);

            VisualElement rightColumn = new VisualElement();
            rightColumn.AddToClassList("provider-right-column");

            Label modelLabel = new Label(LocalizationManager.Get(LocalizationKeys.COMMON_MODEL) + ":");
            modelLabel.AddToClassList("provider-model-label");

            List<BgRemovalModelInfo> modelInfos = GetAllModelInfos(providerType_);
            Dictionary<string, string> displayToId = new Dictionary<string, string>();
            List<string> displayNames = new List<string>();

            foreach (BgRemovalModelInfo modelInfo in modelInfos)
            {
                string displayText = CreateModelDisplayText(modelInfo, _providerSelection.CurrentLanguageCode);
                displayNames.Add(displayText);
                displayToId[displayText] = modelInfo.Id;
            }
            _providerSelection.ModelDisplayToId[providerType_] = displayToId;

            string savedModelId = GetPrimarySelectedModel(providerType_);
            int selectedIndex = 0;
            bool foundSavedModel = false;
            for (int i = 0; i < modelInfos.Count; i++)
            {
                if (modelInfos[i].Id == savedModelId)
                {
                    selectedIndex = i;
                    foundSavedModel = true;
                    break;
                }
            }

            if (!foundSavedModel && modelInfos.Count > 0)
            {
                selectedIndex = 0;
                SaveSelectedModelId(providerType_, modelInfos[0].Id);
            }

            DropdownField modelDropdown = new DropdownField(displayNames, selectedIndex);
            modelDropdown.AddToClassList("provider-model-dropdown");
            modelDropdown.SetEnabled(hasKey);
            modelDropdown.RegisterValueChangedCallback(evt_ =>
            {
                string modelId = _providerSelection.GetModelIdFromDisplay(providerType_, evt_.newValue);
                SaveSelectedModelId(providerType_, modelId);
                UpdateSelectedModelChips();
            });
            _providerSelection.ProviderModels[providerType_] = modelDropdown;

            VisualElement dropdownWrapper = new VisualElement();
            dropdownWrapper.AddToClassList("reorder-dropdown-wrapper");
            dropdownWrapper.Add(modelDropdown);

            if (!hasKey)
            {
                modelDropdown.AddToClassList("reorder-dropdown--disabled");
                dropdownWrapper.RegisterCallback<ClickEvent>(evt_ =>
                {
                    ShowApiKeyRequiredWarningInternal(providerType_);
                    evt_.StopPropagation();
                });
            }
            else if (displayNames.Count == 0)
            {
                modelDropdown.SetEnabled(false);
                modelDropdown.AddToClassList("reorder-dropdown--disabled");
                dropdownWrapper.RegisterCallback<ClickEvent>(evt_ =>
                {
                    ShowNoModelsEnabledWarning(providerType_);
                    evt_.StopPropagation();
                });
            }

            rightColumn.Add(modelLabel);
            rightColumn.Add(dropdownWrapper);

            entry.Add(leftColumn);
            entry.Add(rightColumn);

            return entry;
        }

        private void UpdateProviderModel(BgRemovalProviderType providerType_, string model_)
        {
            _bgRemovalManager?.SetProviderDefaultModel(providerType_, model_);
        }

        private void SaveSelectedModelId(BgRemovalProviderType providerType_, string modelId_)
        {
            UpdateProviderModel(providerType_, modelId_);
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            manager?.SetSelectedModelId(providerType_, modelId_);
        }

        private void ShowNoModelsEnabledWarning(BgRemovalProviderType providerType_)
        {
            string message = LocalizationManager.Get(LocalizationKeys.BGREMOVAL_NO_MODELS_ENABLED, providerType_.ToString());
            SetStatus(message);

            bool goToSettings = EditorUtility.DisplayDialog(
                LocalizationManager.Get(LocalizationKeys.BGREMOVAL_NO_MODELS_ENABLED_TITLE),
                message,
                LocalizationManager.Get(LocalizationKeys.BGREMOVAL_GO_TO_SETTINGS),
                LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
            );

            if (goToSettings)
            {
                OnNavigateToSettingsRequested?.Invoke();
            }
        }

        private void SetStatus(string message_)
        {
            OnStatusChanged?.Invoke(message_);
        }

        private void ShowApiKeyRequiredWarningInternal(BgRemovalProviderType providerType_)
        {
            bool goToSettings = EditorUtility.DisplayDialog(
                LocalizationManager.Get(LocalizationKeys.BGREMOVAL_API_KEY_REQUIRED_TITLE),
                LocalizationManager.Get(LocalizationKeys.BGREMOVAL_API_KEY_REQUIRED, providerType_.ToString()),
                LocalizationManager.Get(LocalizationKeys.BGREMOVAL_GO_TO_SETTINGS),
                LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
            );

            if (goToSettings)
            {
                OnNavigateToSettingsRequested?.Invoke();
            }
        }

        private int GetDefaultPriority(BgRemovalProviderType providerType_)
        {
            return providerType_ switch
            {
                BgRemovalProviderType.REMOVE_BG => 100,
                _ => 0
            };
        }

        private string CreateModelDisplayText(BgRemovalModelInfo modelInfo_, string languageCode_)
        {
            string displayName = !string.IsNullOrEmpty(modelInfo_.DisplayName)
                ? modelInfo_.DisplayName
                : modelInfo_.Id.Replace("/", "\u2215");
            string price = modelInfo_.GetPriceDisplay();
            if (!string.IsNullOrEmpty(price))
            {
                price = price.Replace("/", "\u2215");
                return $"{displayName} ({price})";
            }
            return displayName;
        }

        private List<BgRemovalModelInfo> GetAllModelInfos(BgRemovalProviderType providerType_)
        {
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            return manager != null ? manager.GetAllModelInfos(providerType_) : new List<BgRemovalModelInfo>();
        }

        private BgRemovalModelInfo GetModelInfo(BgRemovalProviderType providerType_, string modelId_)
        {
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            return manager != null ? manager.GetModelInfo(providerType_, modelId_) : null;
        }

        private void AddCustomModel(BgRemovalProviderType providerType_, BgRemovalModelInfo modelInfo_)
        {
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            manager?.AddCustomModel(providerType_, modelInfo_);
        }

        private void RemoveCustomModel(BgRemovalProviderType providerType_, string modelId_)
        {
            EditorProviderManagerBgRemoval manager = GetEditorProviderManager();
            manager?.RemoveCustomModel(providerType_, modelId_);
        }

        private EditorProviderManagerBgRemoval GetEditorProviderManager()
        {
            if (_editorProviderManager != null)
                return _editorProviderManager;

            if (_storage == null)
                return null;

            _editorProviderManager = new EditorProviderManagerBgRemoval(_storage);
            _providerSelection.SetModelState(_editorProviderManager);
            return _editorProviderManager;
        }

        private bool GetProviderEnabledStateInternal(BgRemovalProviderType providerType_)
        {
            if (_providerSelection.ProviderToggles.TryGetValue(providerType_, out Toggle toggle) && toggle != null)
            {
                return toggle.value;
            }

            return false;
        }

        public void UpdateSelectedModelChips()
        {
            _providerSelection.UpdateSelectedModelChips();
        }

        public void OnHide()
        {
            _providerSelection?.OnHide();
        }

        public void Dispose()
        {
            _providerSelection.Dispose();
        }
    }
}
