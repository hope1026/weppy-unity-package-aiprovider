using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ChatProviderSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "Chat/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "ChatProviderSection/ChatProviderSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "ChatProviderSection/ChatProviderSection.uss";

        public event Action<string> OnStatusChanged;

        public event Action OnProviderOrderChanged;
        public event Action<ChatEditorProviderType, bool> OnProviderEnabledChanged;
        public event Action<ChatEditorProviderType, string> OnModelChanged;
        public event Action OnNavigateToSettingsRequested;
        public event Action OnAuthChanged;

        private EditorDataStorage _storage;
        private EditorProviderManagerChat _editorProviderManager;
        private ProviderModelSelectionElement<ChatEditorProviderType, ChatModelInfo> _providerSelection;

        public ChatProviderSection()
        {
            LoadLayout();
            LoadStyles();
            SetupProviderSelection();
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
            ProviderModelSelectionConfig<ChatEditorProviderType, ChatModelInfo> config =
                new ProviderModelSelectionConfig<ChatEditorProviderType, ChatModelInfo>
                {
                    RootContainerClassName = "chat-view-container",
                    ProviderOrderStorageKey = "chat_provider_order",
                    SectionUssPath = USS_PATH,
                    IsNoneProvider = providerType_ => providerType_ == ChatEditorProviderType.NONE,
                    HasProviderAuth = HasProviderAuth,
                    IsProviderEnabled = GetProviderEnabledStateInternal,
                    IsApiKeyOptional = IsApiKeyOptional,
                    IsApiKeyEnabled = IsApiKeyEnabled,
                    SetApiKeyEnabled = SetApiKeyEnabled,
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
                    GetNoModelsEnabledLabelText = _ => LocalizationManager.Get(LocalizationKeys.CHAT_NO_MODELS_ENABLED_TITLE),
                    GetModelsUrl = providerType_ =>
                    {
                        EditorProviderManagerChat manager = GetEditorProviderManager();
                        return manager != null ? manager.GetModelsUrl(providerType_) : string.Empty;
                    },
                    NotifyProviderOrderChanged = () => OnProviderOrderChanged?.Invoke(),
                    NotifyProviderEnabledChanged = (providerType_, enabled_) =>
                    {
                        if (_providerSelection.ProviderToggles.TryGetValue(providerType_, out Toggle toggle) && toggle != null)
                        {
                            toggle.SetValueWithoutNotify(enabled_);
                        }

                        _editorProviderManager?.SetProviderEnabled(providerType_, enabled_);
                        UpdateSelectedModelChips();
                        OnProviderEnabledChanged?.Invoke(providerType_, enabled_);
                    },
                    NotifyModelChanged = (providerType_, modelId_) => OnModelChanged?.Invoke(providerType_, modelId_),
                    GetModelIdFromInfo = modelInfo_ => modelInfo_.Id,
                    GetContextWindowSize = modelInfo_ => modelInfo_.ContextWindowSize,
                    GetMaxOutputTokens = modelInfo_ => modelInfo_.MaxOutputTokens,
                    GetModelPrice = modelInfo_ => modelInfo_.InputPricePerMillion ?? 0,
                    AddCustomModel = (providerType_, modelInfo_) =>
                    {
                        AddCustomModel(providerType_, modelInfo_);
                    },
                    CreateCustomModelInfo = providerType_ => new ChatModelInfo(),
                    RemoveCustomModel = (providerType_, modelId_) =>
                    {
                        RemoveCustomModel(providerType_, modelId_);
                    }
                };

            _providerSelection = new ProviderModelSelectionElement<ChatEditorProviderType, ChatModelInfo>(
                this,
                config,
                _editorProviderManager);
            _providerSelection.SetupUI("model-selection-button");
        }

        public void Initialize(
            EditorDataStorage storage_,
            EditorProviderManagerChat editorProviderManager_,
            string languageCode_)
        {
            _storage = storage_;
            _editorProviderManager = editorProviderManager_;
            _providerSelection.Initialize(storage_, languageCode_);
            _providerSelection.SetModelState(_editorProviderManager);
            _providerSelection.OnAuthChanged += HandleAuthChanged;
            PopulateProviderList();
            UpdateSelectedModelChips();
        }

        public void UpdateLanguage(string languageCode_)
        {
            _providerSelection.UpdateLanguage(languageCode_);
            UpdateSelectedModelChips();
        }

        public void RefreshProviders()
        {
            PopulateProviderList();
            UpdateSelectedModelChips();
        }

        private void PopulateProviderList()
        {
            _providerSelection.ProviderToggles.Clear();
            _providerSelection.ProviderPriorities.Clear();
            _providerSelection.ProviderModels.Clear();
            _providerSelection.ModelDisplayToId.Clear();

            if (_providerSelection.DragDropController == null)
            {
                _providerSelection.InitializeDragDropController(
                    new VisualElement(),
                    HasProviderAuth,
                    HandleProviderOrderChanged,
                    ShowApiKeyRequiredWarningInternal
                );
            }

            List<ChatEditorProviderType> providerOrder = _providerSelection.LoadProviderOrder();
            List<ChatEditorProviderType> sortedOrder = _providerSelection.SortProvidersByEnabledState(providerOrder);
            if (_providerSelection.DragDropController != null)
            {
                _providerSelection.DragDropController.Initialize(sortedOrder);

                foreach (ChatEditorProviderType providerType in _providerSelection.DragDropController.ItemOrder)
                {
                    bool hasKey = HasProviderAuth(providerType);
                    Toggle toggle = new Toggle();
                    toggle.value = hasKey;
                    _providerSelection.ProviderToggles[providerType] = toggle;
                }
            }

            _providerSelection.UpdatePrioritiesFromOrder();
        }

        private void HandleProviderOrderChanged()
        {
            _providerSelection.SaveProviderOrder();
            _providerSelection.UpdatePrioritiesFromOrder();
            UpdateSelectedModelChips();
            OnProviderOrderChanged?.Invoke();
        }

        private void ShowApiKeyRequiredWarningInternal(ChatEditorProviderType providerType_)
        {
            string message = LocalizationManager.Get(LocalizationKeys.CHAT_API_KEY_REQUIRED, providerType_.ToString());
            SetStatus(message);

            bool goToSettings = EditorUtility.DisplayDialog(
                LocalizationManager.Get(LocalizationKeys.CHAT_API_KEY_REQUIRED_TITLE),
                LocalizationManager.Get(LocalizationKeys.CHAT_API_KEY_REQUIRED, providerType_.ToString()),
                LocalizationManager.Get(LocalizationKeys.CHAT_GO_TO_SETTINGS),
                LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
            );

            if (goToSettings)
            {
                OnNavigateToSettingsRequested?.Invoke();
            }
        }

        public string GetPrimarySelectedModel(ChatEditorProviderType providerType_)
        {
            return _editorProviderManager != null ? _editorProviderManager.GetPrimarySelectedModelId(providerType_) : string.Empty;
        }

        public List<ChatEditorProviderTarget> BuildProviderTargets()
        {
            List<ChatEditorProviderTarget> targets = new List<ChatEditorProviderTarget>();
            if (_providerSelection == null)
                return targets;

            IReadOnlyList<ChatEditorProviderType> providerOrder = _providerSelection.DragDropController != null
                ? _providerSelection.DragDropController.ItemOrder
                : _providerSelection.LoadProviderOrder();

            for (int i = 0; i < providerOrder.Count; i++)
            {
                ChatEditorProviderType providerType = providerOrder[i];
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

                targets.Add(new ChatEditorProviderTarget
                {
                    ProviderType = providerType,
                    Model = GetPrimarySelectedModel(providerType),
                    Priority = priority
                });
            }

            return targets;
        }

        public ChatEditorProviderType GetHighestPriorityProvider()
        {
            List<ChatEditorProviderTarget> targets = BuildProviderTargets();
            if (targets.Count == 0)
                return ChatEditorProviderType.NONE;

            return targets[0].ProviderType;
        }

        public void ShowApiKeyInput()
        {
            _providerSelection?.ShowApiKeyInputForFirstProviderWithoutKey();
        }

        private IReadOnlyList<string> GetSelectedModelIds(ChatEditorProviderType providerType_)
        {
            EditorProviderManagerChat manager = GetEditorProviderManager();
            return manager != null ? manager.GetSelectedModelIds(providerType_) : new List<string>();
        }

        private void SaveSelectedModelIds(ChatEditorProviderType providerType_, List<string> modelIds_)
        {
            EditorProviderManagerChat manager = GetEditorProviderManager();
            manager?.SetSelectedModelIds(providerType_, modelIds_);
        }

        private void SaveSelectedModelId(ChatEditorProviderType providerType_, string modelId_)
        {
            EditorProviderManagerChat manager = GetEditorProviderManager();
            manager?.SetSelectedModelId(providerType_, modelId_);
        }

        private List<ChatModelInfo> GetAllModelInfos(ChatEditorProviderType providerType_)
        {
            EditorProviderManagerChat manager = GetEditorProviderManager();
            return manager != null ? manager.GetAllModelInfos(providerType_) : new List<ChatModelInfo>();
        }

        private ChatModelInfo GetModelInfo(ChatEditorProviderType providerType_, string modelId_)
        {
            EditorProviderManagerChat manager = GetEditorProviderManager();
            return manager != null ? manager.GetModelInfo(providerType_, modelId_) : null;
        }

        private void AddCustomModel(ChatEditorProviderType providerType_, ChatModelInfo modelInfo_)
        {
            EditorProviderManagerChat manager = GetEditorProviderManager();
            manager?.AddCustomModel(providerType_, modelInfo_);
        }

        private void RemoveCustomModel(ChatEditorProviderType providerType_, string modelId_)
        {
            EditorProviderManagerChat manager = GetEditorProviderManager();
            manager?.RemoveCustomModel(providerType_, modelId_);
        }

        private EditorProviderManagerChat GetEditorProviderManager()
        {
            if (_editorProviderManager != null)
                return _editorProviderManager;

            if (_storage == null)
                return null;

            _editorProviderManager = new EditorProviderManagerChat(_storage);
            _providerSelection.SetModelState(_editorProviderManager);
            return _editorProviderManager;
        }

        private string CreateModelDisplayText(ChatModelInfo chatModelInfo_, string languageCode_)
        {
            string displayName = chatModelInfo_.DisplayName.Replace("/", "\u2215");
            string modelId = chatModelInfo_.Id.Replace("/", "\u2215");
            string display = $"{displayName} ({modelId})";
            string description = chatModelInfo_.GetDescription(languageCode_);
            if (!string.IsNullOrEmpty(description))
            {
                description = description.Replace("/", "\u2215");
                display += $" - {description}";
            }

            string price = chatModelInfo_.GetPriceDisplay();
            if (!string.IsNullOrEmpty(price))
            {
                price = price.Replace("/", "|");
                display += $" [{price}]";
            }

            return display;
        }

        public void UpdateSelectedModelChips()
        {
            _providerSelection.UpdateSelectedModelChips();
        }

        public bool HasEnabledProvider()
        {
            foreach (ChatEditorProviderType providerType in Enum.GetValues(typeof(ChatEditorProviderType)))
            {
                if (providerType == ChatEditorProviderType.NONE)
                    continue;

                if (!HasProviderAuth(providerType))
                    continue;

                if (!GetProviderEnabledStateInternal(providerType))
                    continue;

                return true;
            }

            return false;
        }

        public void OnHide()
        {
            _providerSelection?.OnHide();
        }

        private void HandleAuthChanged()
        {
            OnAuthChanged?.Invoke();
        }

        public void Dispose()
        {
            _providerSelection.Dispose();
        }

        private void SetStatus(string message_)
        {
            OnStatusChanged?.Invoke(message_);
        }

        private bool GetProviderEnabledStateInternal(ChatEditorProviderType providerType_)
        {
            if (_providerSelection.ProviderToggles.TryGetValue(providerType_, out Toggle toggle) && toggle != null)
            {
                return toggle.value;
            }

            return false;
        }

        private bool IsApiKeyOptional(ChatEditorProviderType providerType_)
        {
            return providerType_ == ChatEditorProviderType.CODEX_CLI ||
                   providerType_ == ChatEditorProviderType.CLAUDE_CODE_CLI ||
                   providerType_ == ChatEditorProviderType.GEMINI_CLI;
        }

        private bool IsApiKeyEnabled(ChatEditorProviderType providerType_)
        {
            if (!IsApiKeyOptional(providerType_))
                return true;

            return EditorDataStorageKeys.GetCliUseApiKey(_storage, providerType_);
        }

        private void SetApiKeyEnabled(ChatEditorProviderType providerType_, bool enabled_)
        {
            if (!IsApiKeyOptional(providerType_))
                return;

            EditorDataStorageKeys.SetCliUseApiKey(_storage, providerType_, enabled_);
        }

        private bool HasProviderAuth(ChatEditorProviderType providerType_)
        {
            if (providerType_ == ChatEditorProviderType.NONE)
                return false;

            if (IsApiKeyOptional(providerType_))
            {
                string cliPath = EditorDataStorageKeys.GetCliExecutablePath(_storage, providerType_);
                bool isCliPathValid = IsCliExecutablePathValid(cliPath);

                if (!IsApiKeyEnabled(providerType_))
                    return isCliPathValid;

                bool hasApiKey = !string.IsNullOrEmpty(EditorDataStorageKeys.GetCliApiKey(_storage, providerType_));
                return isCliPathValid && hasApiKey;
            }

            return !string.IsNullOrEmpty(EditorDataStorageKeys.GetApiKey(_storage, providerType_));
        }

        private bool IsCliExecutablePathValid(string path_)
        {
            if (string.IsNullOrWhiteSpace(path_))
                return false;

            string trimmedPath = path_.Trim();
            if (Path.IsPathRooted(trimmedPath))
                return File.Exists(trimmedPath);

            return false;
        }
    }
}
