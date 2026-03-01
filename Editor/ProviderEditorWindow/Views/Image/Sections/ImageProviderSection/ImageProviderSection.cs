using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ImageProviderSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "Image/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "ImageProviderSection/ImageProviderSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "ImageProviderSection/ImageProviderSection.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnNavigateToSettingsRequested;
        public event Action OnProvidersChanged;
        public event Action OnActiveModelsChanged;
        public event Action OnAuthChanged;

        private ImageProviderManager _imageGenerationManagerRef;
        private ImageAppProviderManager _imageAppManagerRef;
        private EditorDataStorage _storageRef;
        private EditorProviderManagerImage _editorProviderManagerRef;
        private ProviderModelSelectionElement<ImageEditorProviderType, ImageModelInfo> _providerSelection;
        private VisualElement _providerList;

        public ImageProviderSection()
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
            ProviderModelSelectionConfig<ImageEditorProviderType, ImageModelInfo> config =
                new ProviderModelSelectionConfig<ImageEditorProviderType, ImageModelInfo>
                {
                    RootContainerClassName = "image-view-container",
                    ProviderOrderStorageKey = "image_provider_order",
                    SectionUssPath = USS_PATH,
                    IsNoneProvider = providerType_ => providerType_ == ImageEditorProviderType.NONE,
                    HasProviderAuth = HasProviderAuth,
                    IsProviderEnabled = GetProviderEnabledStateInternal,
                    IsApiKeyOptional = IsApiKeyOptional,
                    IsApiKeyEnabled = IsApiKeyEnabled,
                    SetApiKeyEnabled = SetApiKeyEnabled,
                    GetSelectionButtonText = selectedCount_ =>
                        LocalizationManager.Get(LocalizationKeys.COMMON_MODEL_SELECTION) + $" ({selectedCount_})",
                    GetPrimarySelectedModelId = GetPrimarySelectedModel,
                    GetSelectedModelIds = GetSelectedModelIds,
                    SaveSelectedModelId = SaveSelectedModelId,
                    SaveSelectedModelIds = SaveSelectedModelIds,
                    GetModelInfo = GetModelInfo,
                    GetModelDisplayText = CreateModelDisplayText,
                    GetModelDisplayName = (modelInfo_, fallback_) => modelInfo_ != null ? modelInfo_.DisplayName : fallback_,
                    GetModelDescription = (modelInfo_, languageCode_) => modelInfo_.GetDescription(languageCode_),
                    GetModelPriceDisplay = modelInfo_ => modelInfo_ != null ? modelInfo_.GetPriceDisplay() : string.Empty,
                    GetNoModelsEnabledLabelText = _ => LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NO_MODELS_ENABLED_TITLE),
                    GetModelsUrl = providerType_ => _editorProviderManagerRef != null
                        ? _editorProviderManagerRef.GetModelsUrl(providerType_)
                        : string.Empty,
                    NotifyProviderOrderChanged = () => OnProvidersChanged?.Invoke(),
                    NotifyProviderEnabledChanged = (providerType_, enabled_) =>
                    {
                        if (_providerSelection.ProviderToggles.TryGetValue(providerType_, out Toggle toggle) && toggle != null)
                        {
                            toggle.SetValueWithoutNotify(enabled_);
                        }

                        SetProviderEnabled(providerType_, enabled_);
                        _editorProviderManagerRef?.SetProviderEnabled(providerType_, enabled_);
                        UpdateSelectedModelChips();
                        OnProvidersChanged?.Invoke();
                        OnActiveModelsChanged?.Invoke();
                    },
                    NotifyModelChanged = (providerType_, modelId_) => OnActiveModelsChanged?.Invoke(),
                    GetModelIdFromInfo = modelInfo_ => modelInfo_.Id,
                    GetContextWindowSize = modelInfo_ => modelInfo_.ContextWindowSize,
                    GetMaxOutputTokens = modelInfo_ => modelInfo_.MaxOutputTokens,
                    GetModelPrice = modelInfo_ => modelInfo_?.PricePerImage ?? 0,
                    GetAllModelInfos = GetAllModelInfos,
                    AddCustomModel = (providerType_, modelInfo_) =>
                    {
                        AddCustomModel(providerType_, modelInfo_);
                    },
                    CreateCustomModelInfo = providerType_ => new ImageModelInfo(),
                    RemoveCustomModel = (providerType_, modelId_) =>
                    {
                        RemoveCustomModel(providerType_, modelId_);
                    }
                };

            _providerSelection = new ProviderModelSelectionElement<ImageEditorProviderType, ImageModelInfo>(
                this,
                config,
                _editorProviderManagerRef);
            _providerSelection.SetupUI("image-provider-selection-button");
        }

        public void Initialize(
            EditorDataStorage storage_,
            ImageProviderManager apiManager_,
            ImageAppProviderManager appManager_,
            EditorProviderManagerImage editorProviderManager_,
            string languageCode_)
        {
            _storageRef = storage_;
            _imageGenerationManagerRef = apiManager_;
            _imageAppManagerRef = appManager_;
            _editorProviderManagerRef = editorProviderManager_;
            _providerSelection.Initialize(storage_, languageCode_);
            _providerSelection.SetModelState(_editorProviderManagerRef);
            _providerSelection.OnAuthChanged += HandleAuthChanged;
            PopulateProviderList();
            UpdateSectionHeaders();
        }

        private void HandleAuthChanged()
        {
            OnAuthChanged?.Invoke();
        }

        public void SetManager(ImageProviderManager apiManager_, ImageAppProviderManager appManager_)
        {
            _imageGenerationManagerRef = apiManager_;
            _imageAppManagerRef = appManager_;
            _providerSelection.UpdatePrioritiesFromOrder();
            UpdateSelectedModelChips();
        }

        private void SetupUI()
        {
            _providerList = new VisualElement();
            _providerList.name = "image-provider-list";
            _providerList.AddToClassList("image-provider-list");

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
            foreach (KeyValuePair<ImageEditorProviderType, Toggle> kvp in _providerSelection.ProviderToggles)
            {
                if (kvp.Value != null && kvp.Value.value)
                {
                    if (HasProviderAuth(kvp.Key))
                        return true;
                }
            }

            return false;
        }

        public string GetPrimarySelectedModel(ImageEditorProviderType generationProviderType_)
        {
            return _editorProviderManagerRef != null ? _editorProviderManagerRef.GetPrimarySelectedModelId(generationProviderType_) : string.Empty;
        }

        public List<ImageEditorProviderTarget> BuildProviderTargets()
        {
            List<ImageEditorProviderTarget> targets = new List<ImageEditorProviderTarget>();
            if (_providerSelection == null)
                return targets;

            IReadOnlyList<ImageEditorProviderType> providerOrder = _providerSelection.DragDropController != null
                ? _providerSelection.DragDropController.ItemOrder
                : _providerSelection.LoadProviderOrder();

            for (int i = 0; i < providerOrder.Count; i++)
            {
                ImageEditorProviderType providerType = providerOrder[i];
                if (!HasProviderAuth(providerType))
                    continue;

                if (!GetProviderEnabledStateInternal(providerType))
                    continue;

                IReadOnlyList<string> selectedModelIds = GetSelectedModelIds(providerType);
                if (selectedModelIds == null || selectedModelIds.Count == 0)
                    continue;

                int priority = _providerSelection.DragDropController != null
                    ? _providerSelection.DragDropController.GetPriorityForIndex(i)
                    : 100 - (i * 10);

                targets.Add(new ImageEditorProviderTarget
                {
                    ProviderType = providerType,
                    Model = GetPrimarySelectedModel(providerType),
                    Priority = priority
                });
            }

            return targets;
        }

        public ImageEditorProviderType GetHighestPriorityProvider()
        {
            List<ImageEditorProviderTarget> targets = BuildProviderTargets();
            if (targets.Count == 0)
                return ImageEditorProviderType.NONE;

            return targets[0].ProviderType;
        }

        public void ShowApiKeyInput()
        {
            _providerSelection?.ShowApiKeyInputForFirstProviderWithoutKey();
        }

        private IReadOnlyList<string> GetSelectedModelIds(ImageEditorProviderType providerType_)
        {
            return _editorProviderManagerRef != null ? _editorProviderManagerRef.GetSelectedModelIds(providerType_) : new List<string>();
        }

        private void SaveSelectedModelIds(ImageEditorProviderType providerType_, List<string> modelIds_)
        {
            if (_editorProviderManagerRef != null)
            {
                _editorProviderManagerRef.SetSelectedModelIds(providerType_, modelIds_);
                return;
            }

            if (_storageRef == null)
                return;

            string key = EditorDataStorageKeys.GetSelectedImageModelListKey(providerType_);
            if (string.IsNullOrEmpty(key))
                return;

            string serialized = SerializeModelIdList(modelIds_);
            _storageRef.SetString(key, serialized);
            _storageRef.Save();
        }

        private List<ImageModelInfo> GetAllModelInfos(ImageEditorProviderType providerType_)
        {
            return _editorProviderManagerRef != null ? _editorProviderManagerRef.GetAllModelInfos(providerType_) : new List<ImageModelInfo>();
        }

        private ImageModelInfo GetModelInfo(ImageEditorProviderType providerType_, string modelId_)
        {
            return _editorProviderManagerRef != null ? _editorProviderManagerRef.GetModelInfo(providerType_, modelId_) : null;
        }

        private void AddCustomModel(ImageEditorProviderType providerType_, ImageModelInfo modelInfo_)
        {
            _editorProviderManagerRef?.AddCustomModel(providerType_, modelInfo_);
        }

        private void RemoveCustomModel(ImageEditorProviderType providerType_, string modelId_)
        {
            _editorProviderManagerRef?.RemoveCustomModel(providerType_, modelId_);
        }

        public List<ImageModelInfo> GetActiveModelInfos()
        {
            List<ImageModelInfo> activeModels = new List<ImageModelInfo>();
            foreach (KeyValuePair<ImageEditorProviderType, Toggle> kvp in _providerSelection.ProviderToggles)
            {
                if (kvp.Value != null && kvp.Value.value)
                {
                    ImageEditorProviderType providerType = kvp.Key;
                    IReadOnlyList<string> selectedModelIds = _editorProviderManagerRef.GetSelectedModelIds(providerType);
                    foreach (string selectedModelId in selectedModelIds)
                    {
                        if (!string.IsNullOrEmpty(selectedModelId))
                        {
                            ImageModelInfo generationModelInfo = GetModelInfo(providerType, selectedModelId);
                            if (generationModelInfo != null)
                            {
                                activeModels.Add(generationModelInfo);
                            }
                        }
                    }
                }
            }

            return activeModels;
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
                HasProviderAuth,
                OnProviderOrderChanged,
                ShowApiKeyRequiredWarningInternal
            );

            List<ImageEditorProviderType> providerOrder = _providerSelection.LoadProviderOrder();
            _providerSelection.DragDropController.Initialize(providerOrder);

            foreach (ImageEditorProviderType providerType in _providerSelection.DragDropController.ItemOrder)
            {
                VisualElement entry = CreateProviderEntry(providerType);
                _providerSelection.DragDropController.SetupEntry(entry, providerType);
                _providerList.Add(entry);
            }

            _providerSelection.UpdatePrioritiesFromOrder();
            UpdateSelectedModelChips();
            OnProvidersChanged?.Invoke();
            OnActiveModelsChanged?.Invoke();
        }

        private void OnProviderOrderChanged()
        {
            _providerSelection.SaveProviderOrder();
            _providerSelection.UpdatePrioritiesFromOrder();
            OnProvidersChanged?.Invoke();
        }

        private VisualElement CreateProviderEntry(ImageEditorProviderType generationProviderType_)
        {
            VisualElement entry = new VisualElement();
            entry.AddToClassList("provider-entry");
            entry.AddToClassList("provider-entry-two-column");

            bool hasKey = HasProviderAuth(generationProviderType_);

            VisualElement leftColumn = new VisualElement();
            leftColumn.AddToClassList("provider-left-column");

            Toggle toggle = new Toggle();
            toggle.AddToClassList("provider-enabled-toggle");
            toggle.value = hasKey;
            _editorProviderManagerRef?.SetProviderEnabled(generationProviderType_, toggle.value);

            VisualElement toggleWrapper = new VisualElement();
            toggleWrapper.AddToClassList("reorder-toggle-wrapper");
            toggleWrapper.Add(toggle);

            if (!hasKey)
            {
                toggle.SetEnabled(false);
                toggleWrapper.RegisterCallback<ClickEvent>(evt =>
                {
                    ShowApiKeyRequiredWarningInternal(generationProviderType_);
                    evt.StopPropagation();
                });
            }
            else
            {
                toggle.RegisterValueChangedCallback(evt =>
                {
                    SetProviderEnabled(generationProviderType_, evt.newValue);
                    _editorProviderManagerRef?.SetProviderEnabled(generationProviderType_, evt.newValue);
                    UpdateSelectedModelChips();
                    OnProvidersChanged?.Invoke();
                    OnActiveModelsChanged?.Invoke();
                });
            }

            _providerSelection.ProviderToggles[generationProviderType_] = toggle;

            Label nameLabel = new Label(generationProviderType_.ToString());
            nameLabel.AddToClassList("provider-name");

            Label priorityLabel = new Label(LocalizationManager.Get(LocalizationKeys.COMMON_PRIORITY) + ":");
            priorityLabel.AddToClassList("provider-priority-label");

            IntegerField priorityField = new IntegerField();
            priorityField.AddToClassList("provider-priority");
            priorityField.AddToClassList("provider-priority-readonly");
            priorityField.value = GetDefaultPriority(generationProviderType_);
            priorityField.SetEnabled(false);
            _providerSelection.ProviderPriorities[generationProviderType_] = priorityField;

            Label statusLabel = new Label();
            statusLabel.AddToClassList("provider-status");
            UpdateProviderStatusLabel(statusLabel, generationProviderType_);

            if (!hasKey)
            {
                statusLabel.AddToClassList("provider-status-clickable");
                statusLabel.RegisterCallback<ClickEvent>(evt => { ShowApiKeyRequiredWarningInternal(generationProviderType_); });
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

            List<ImageModelInfo> modelInfos = GetAllModelInfos(generationProviderType_);
            Dictionary<string, string> displayToId = new Dictionary<string, string>();
            List<string> displayNames = new List<string>();

            foreach (ImageModelInfo modelInfo in modelInfos)
            {
                string displayText = CreateModelDisplayText(modelInfo, _providerSelection.CurrentLanguageCode);
                displayNames.Add(displayText);
                displayToId[displayText] = modelInfo.Id;
            }

            _providerSelection.ModelDisplayToId[generationProviderType_] = displayToId;

            string savedModelId = GetPrimarySelectedModel(generationProviderType_);
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
                if (_editorProviderManagerRef != null)
                {
                    _editorProviderManagerRef.SetSelectedModelId(generationProviderType_, modelInfos[0].Id);
                }
                else
                {
                    string modelKey = EditorDataStorageKeys.GetSelectedImageModelKey(generationProviderType_);
                    if (_storageRef != null && !string.IsNullOrEmpty(modelKey))
                    {
                        _storageRef.SetString(modelKey, modelInfos[0].Id);
                        _storageRef.Save();
                    }
                }
            }

            DropdownField modelDropdown = new DropdownField(displayNames, selectedIndex);
            modelDropdown.AddToClassList("provider-model-dropdown");

            VisualElement dropdownWrapper = new VisualElement();
            dropdownWrapper.AddToClassList("reorder-dropdown-wrapper");
            dropdownWrapper.Add(modelDropdown);

            if (!hasKey)
            {
                modelDropdown.SetEnabled(false);
                modelDropdown.AddToClassList("reorder-dropdown--disabled");
                dropdownWrapper.RegisterCallback<ClickEvent>(evt =>
                {
                    ShowApiKeyRequiredWarningInternal(generationProviderType_);
                    evt.StopPropagation();
                });
            }
            else if (displayNames.Count == 0)
            {
                modelDropdown.SetEnabled(false);
                modelDropdown.AddToClassList("reorder-dropdown--disabled");
                dropdownWrapper.RegisterCallback<ClickEvent>(evt =>
                {
                    ShowNoModelsEnabledWarning(generationProviderType_);
                    evt.StopPropagation();
                });
            }
            else
            {
                modelDropdown.RegisterValueChangedCallback(evt =>
                {
                    string modelId = _providerSelection.GetModelIdFromDisplay(generationProviderType_, evt.newValue);
                    UpdateProviderModel(generationProviderType_, modelId);
                    UpdateSelectedModelChips();
                    OnActiveModelsChanged?.Invoke();
                });
            }

            _providerSelection.ProviderModels[generationProviderType_] = modelDropdown;

            rightColumn.Add(modelLabel);
            rightColumn.Add(dropdownWrapper);

            entry.Add(leftColumn);
            entry.Add(rightColumn);

            return entry;
        }

        private void ShowNoModelsEnabledWarning(ImageEditorProviderType generationProviderType_)
        {
            string message = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NO_MODELS_ENABLED, generationProviderType_.ToString());
            SetStatus(message);

            bool goToSettings = EditorUtility.DisplayDialog(
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NO_MODELS_ENABLED_TITLE),
                message,
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GO_TO_SETTINGS),
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

        private void ShowApiKeyRequiredWarningInternal(ImageEditorProviderType generationProviderType_)
        {
            string message = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_API_KEY_REQUIRED, generationProviderType_.ToString());
            SetStatus(message);

            bool goToSettings = EditorUtility.DisplayDialog(
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_API_KEY_REQUIRED_TITLE),
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_API_KEY_REQUIRED, generationProviderType_.ToString()),
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GO_TO_SETTINGS),
                LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
            );

            if (goToSettings)
            {
                OnNavigateToSettingsRequested?.Invoke();
            }
        }

        private void UpdateProviderModel(ImageEditorProviderType generationProviderType_, string model_)
        {
            if (ImageEditorProviderTypeUtility.IsApiProvider(generationProviderType_))
            {
                ImageProviderType apiProviderType = ImageEditorProviderTypeUtility.ToApiProviderType(generationProviderType_);
                _imageGenerationManagerRef?.SetProviderDefaultModel(apiProviderType, model_);
                return;
            }

            if (ImageEditorProviderTypeUtility.IsAppProvider(generationProviderType_))
            {
                ImageAppProviderType appProviderType = ImageEditorProviderTypeUtility.ToAppProviderType(generationProviderType_);
                _imageAppManagerRef?.SetProviderDefaultModel(appProviderType, model_);
            }
        }

        private void SaveSelectedModelId(ImageEditorProviderType providerType_, string modelId_)
        {
            UpdateProviderModel(providerType_, modelId_);

            if (_editorProviderManagerRef != null)
            {
                _editorProviderManagerRef.SetSelectedModelId(providerType_, modelId_);
                return;
            }

            string modelKey = EditorDataStorageKeys.GetSelectedImageModelKey(providerType_);
            if (_storageRef != null && !string.IsNullOrEmpty(modelKey))
            {
                _storageRef.SetString(modelKey, modelId_);
                _storageRef.Save();
            }
        }

        private List<string> ParseModelIdList(string serialized_)
        {
            List<string> modelIds = new List<string>();
            if (string.IsNullOrEmpty(serialized_))
            {
                return modelIds;
            }

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
            {
                return string.Empty;
            }

            return string.Join("|", modelIds_);
        }

        private bool GetProviderEnabledStateInternal(ImageEditorProviderType providerType_)
        {
            if (_providerSelection.ProviderToggles.TryGetValue(providerType_, out Toggle toggle) && toggle != null)
            {
                return toggle.value;
            }

            return false;
        }

        private bool HasProviderAuth(ImageEditorProviderType providerType_)
        {
            if (providerType_ == ImageEditorProviderType.NONE)
                return false;

            if (IsApiKeyOptional(providerType_))
            {
                string executablePath = EditorDataStorageKeys.GetImageAppExecutablePath(_storageRef, providerType_);
                bool isExecutablePathValid = IsAppExecutablePathValid(executablePath);
                if (!IsApiKeyEnabled(providerType_))
                    return isExecutablePathValid;

                bool hasApiKey = !string.IsNullOrEmpty(EditorDataStorageKeys.GetImageAppApiKey(_storageRef, providerType_));
                return isExecutablePathValid && hasApiKey;
            }

            return !string.IsNullOrEmpty(EditorDataStorageKeys.GetApiKey(_storageRef, providerType_));
        }

        private void SetProviderEnabled(ImageEditorProviderType providerType_, bool enabled_)
        {
            if (ImageEditorProviderTypeUtility.IsApiProvider(providerType_))
            {
                ImageProviderType apiProviderType = ImageEditorProviderTypeUtility.ToApiProviderType(providerType_);
                _imageGenerationManagerRef?.SetProviderEnabled(apiProviderType, enabled_);
                return;
            }

            if (ImageEditorProviderTypeUtility.IsAppProvider(providerType_))
            {
                ImageAppProviderType appProviderType = ImageEditorProviderTypeUtility.ToAppProviderType(providerType_);
                _imageAppManagerRef?.SetProviderEnabled(appProviderType, enabled_);
            }
        }

        private void UpdateProviderStatusLabel(Label statusLabel_, ImageEditorProviderType providerType_)
        {
            if (statusLabel_ == null)
                return;

            bool isOptional = IsApiKeyOptional(providerType_);
            bool useApiKey = !isOptional || IsApiKeyEnabled(providerType_);
            bool hasStoredKey = useApiKey
                ? !string.IsNullOrEmpty(EditorDataStorageKeys.GetApiKey(_storageRef, providerType_))
                : IsAppExecutablePathValid(EditorDataStorageKeys.GetImageAppExecutablePath(_storageRef, providerType_));

            statusLabel_.RemoveFromClassList("key-status-set");
            statusLabel_.RemoveFromClassList("key-status-missing");
            statusLabel_.RemoveFromClassList("key-status-optional");

            if (isOptional && !useApiKey)
            {
                statusLabel_.text = LocalizationManager.Get(LocalizationKeys.API_KEY_OPTIONAL_STATUS);
                statusLabel_.AddToClassList("key-status-optional");
                return;
            }

            if (hasStoredKey)
            {
                statusLabel_.text = LocalizationManager.Get(LocalizationKeys.SETTINGS_API_KEY_SET);
                statusLabel_.AddToClassList("key-status-set");
            }
            else
            {
                statusLabel_.text = LocalizationManager.Get(LocalizationKeys.SETTINGS_NO_API_KEY);
                statusLabel_.AddToClassList("key-status-missing");
            }
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

        private bool IsApiKeyOptional(ImageEditorProviderType providerType_)
        {
            return providerType_ == ImageEditorProviderType.CODEX_APP;
        }

        private bool IsApiKeyEnabled(ImageEditorProviderType providerType_)
        {
            if (!IsApiKeyOptional(providerType_))
                return true;

            return EditorDataStorageKeys.GetImageAppUseApiKey(_storageRef, providerType_);
        }

        private void SetApiKeyEnabled(ImageEditorProviderType providerType_, bool enabled_)
        {
            if (!IsApiKeyOptional(providerType_))
                return;

            EditorDataStorageKeys.SetImageAppUseApiKey(_storageRef, providerType_, enabled_);
        }

        private bool IsAppExecutablePathValid(string path_)
        {
            if (string.IsNullOrWhiteSpace(path_))
                return false;

            string trimmedPath = path_.Trim();
            if (Path.IsPathRooted(trimmedPath))
                return File.Exists(trimmedPath);

            return false;
        }

        private int GetDefaultPriority(ImageEditorProviderType generationProviderType_)
        {
            return generationProviderType_ switch
            {
                ImageEditorProviderType.OPEN_AI => 100,
                ImageEditorProviderType.CODEX_APP => 90,
                ImageEditorProviderType.GOOGLE_GEMINI => 60,
                ImageEditorProviderType.GOOGLE_IMAGEN => 50,
                _ => 0
            };
        }

        private string CreateModelDisplayText(ImageModelInfo generationModelInfo_, string languageCode_)
        {
            string modelId = generationModelInfo_.Id.Replace("/", "\u2215");
            string display = $"{generationModelInfo_.DisplayName} ({modelId})";
            string description = generationModelInfo_.GetDescription(languageCode_);
            if (!string.IsNullOrEmpty(description))
            {
                display += $" - {description}";
            }

            string price = generationModelInfo_.GetPriceDisplay();
            if (!string.IsNullOrEmpty(price))
            {
                price = price.Replace("/", "|");
                display += $" [{price}]";
            }

            return display;
        }
    }
}
