using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class BgRemovalView : VisualElement
    {
        private static readonly string UXML_PATH = EditorPaths.VIEWS_PATH + "BgRemoval/BgRemovalView.uxml";
        private static readonly string USS_PATH = EditorPaths.VIEWS_PATH + "BgRemoval/BgRemovalView.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnNavigateToSettingsRequested;

        private EditorDataStorage _storage;
        private BgRemovalProviderManager _bgRemovalManager;
        private EditorProviderManagerBgRemoval _manager;
        private CancellationTokenSource _cancellationTokenSource;

        private VisualElement _noApiKeyWarning;

        private BgRemovalProviderSection _providerSection;
        private BgRemovalInputSection _inputSection;
        private BgRemovalResultsSection _resultsSection;

        private bool _isProcessing;
        private string _currentLanguageCode;

        public BgRemovalView(EditorDataStorage storage_)
        {
            _storage = storage_;
            _manager = new EditorProviderManagerBgRemoval(_storage);
            LoadLayout();
            LoadStyles();
            LoadLanguageSetting();
            SetupUI();
            InitializeSections();
            InitializeManager();
            UpdateUIState();
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
            StyleSheet themeStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorPaths.THEME_STYLES_PATH);
            if (themeStyles != null)
            {
                styleSheets.Add(themeStyles);
            }

            StyleSheet viewStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (viewStyles != null)
            {
                styleSheets.Add(viewStyles);
            }
        }

        private void SetStatus(string message_)
        {
            OnStatusChanged?.Invoke(message_);
        }

        public void OnShow()
        {
        }

        public void OnHide()
        {
            _providerSection?.OnHide();
        }

        public void SaveHistoryIfNeeded()
        {
            _resultsSection?.SaveHistoryIfNeeded();
        }

        private void LoadLanguageSetting()
        {
            _currentLanguageCode = LocalizationManager.CurrentLanguageCode;
        }

        public void RefreshProviders()
        {
            LoadLanguageSetting();
            InitializeManager();
            _providerSection?.RefreshProviders();
            UpdateUIState();
        }

        private void SetupUI()
        {
            CreateNoApiKeyWarning();

            VisualElement providerContainer = this.Q<VisualElement>("provider-section-container");
            VisualElement inputContainer = this.Q<VisualElement>("input-section-container");
            VisualElement resultsContainer = this.Q<VisualElement>("results-section-container");

            _providerSection = new BgRemovalProviderSection();
            _inputSection = new BgRemovalInputSection();
            _resultsSection = new BgRemovalResultsSection();

            if (providerContainer != null)
            {
                providerContainer.Add(_providerSection);
            }
            else
            {
                Add(_providerSection);
            }

            if (inputContainer != null)
            {
                inputContainer.Add(_inputSection);
            }
            else
            {
                Add(_inputSection);
            }

            if (resultsContainer != null)
            {
                resultsContainer.Add(_resultsSection);
            }
            else
            {
                Add(_resultsSection);
            }

            WireSectionEvents();

            LocalizationManager.OnLanguageChanged += UpdateSectionHeaders;
        }

        private void WireSectionEvents()
        {
            if (_providerSection != null)
            {
                _providerSection.OnStatusChanged += SetStatus;
                _providerSection.OnNavigateToSettingsRequested += () => OnNavigateToSettingsRequested?.Invoke();
                _providerSection.OnProvidersChanged += UpdateUIState;
            }

            if (_inputSection != null)
            {
                _inputSection.OnStatusChanged += SetStatus;
                _inputSection.OnProcessRequested += OnProcessClicked;
                _inputSection.OnProcessAllRequested += OnProcessAllClicked;
                _inputSection.OnCancelRequested += OnCancelClicked;
                _inputSection.OnInputImageChanged += UpdateUIState;
                _inputSection.RegisterDisabledWarning(ShowFeatureDisabledWarning);
            }

            if (_resultsSection != null)
            {
                _resultsSection.OnStatusChanged += SetStatus;
            }
        }

        private void InitializeSections()
        {
            _providerSection?.Initialize(_storage, _bgRemovalManager, _manager, _currentLanguageCode);
            _inputSection?.Initialize(_storage);
        }

        private void UpdateSectionHeaders()
        {
            _currentLanguageCode = LocalizationManager.CurrentLanguageCode;
            _providerSection?.UpdateLanguage(_currentLanguageCode);
            _inputSection?.UpdateLocalization();
        }

        public void InitializeManager()
        {
            _bgRemovalManager?.Dispose();
            _bgRemovalManager = new BgRemovalProviderManager();

            if (_storage == null)
                return;

            string removeBgKey = _storage.GetString(EditorDataStorageKeys.KEY_REMOVEBG);

            if (!string.IsNullOrEmpty(removeBgKey))
            {
                string model = _providerSection?.GetPrimarySelectedModel(BgRemovalProviderType.REMOVE_BG) ?? string.Empty;
                if (string.IsNullOrEmpty(model) && _manager != null)
                    model = _manager.GetDefaultBgRemovalModel(BgRemovalProviderType.REMOVE_BG);
                BgRemovalProviderSettings settings = new BgRemovalProviderSettings(removeBgKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                _bgRemovalManager.AddProvider(BgRemovalProviderType.REMOVE_BG, settings);
            }

            _providerSection?.SetManager(_bgRemovalManager);
        }

        private void SetProcessing(bool isProcessing_, string message_ = "Processing...")
        {
            _isProcessing = isProcessing_;
            _inputSection?.SetProcessing(isProcessing_, message_);
            UpdateUIState();
        }

        private void OnCancelClicked()
        {
            _cancellationTokenSource?.Cancel();
            SetProcessing(false);
            SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_CANCELLED));
        }

        private void UpdateUIState()
        {
            bool hasAnyKey = EditorDataStorageKeys.HasAnyBgRemovalProviderKey(_storage);
            bool hasEnabledProvider = HasEnabledProvider();
            bool hasInputImage = _inputSection?.HasInputImage() ?? false;
            bool canAddImage = hasAnyKey && hasEnabledProvider;
            bool canExecute = canAddImage && hasInputImage;

            if (_noApiKeyWarning != null)
                _noApiKeyWarning.style.display = hasAnyKey ? DisplayStyle.None : DisplayStyle.Flex;

            _inputSection?.SetActionButtonsEnabled(canAddImage && !_isProcessing, canExecute && !_isProcessing);
        }

        private bool HasEnabledProvider()
        {
            return _providerSection?.HasEnabledProvider() ?? false;
        }

        private string GetDisabledReason()
        {
            bool hasAnyKey = EditorDataStorageKeys.HasAnyBgRemovalProviderKey(_storage);
            if (!hasAnyKey)
            {
                return LocalizationManager.Get(LocalizationKeys.BGREMOVAL_NO_API_KEYS);
            }

            if (!HasEnabledProvider())
            {
                return LocalizationManager.Get(LocalizationKeys.BGREMOVAL_NO_ENABLED_PROVIDER);
            }

            if (_inputSection == null || !_inputSection.HasInputImage())
            {
                return LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PLEASE_SELECT_IMAGE);
            }

            return string.Empty;
        }

        private void ShowFeatureDisabledWarning()
        {
            string reason = GetDisabledReason();
            if (string.IsNullOrEmpty(reason))
                return;

            SetStatus(reason);

            if (_inputSection == null || !_inputSection.HasInputImage() || !EditorDataStorageKeys.HasAnyBgRemovalProviderKey(_storage) || !HasEnabledProvider())
            {
                bool goToSettings = EditorUtility.DisplayDialog(
                    LocalizationManager.Get(LocalizationKeys.BGREMOVAL_FEATURE_DISABLED_TITLE),
                    reason,
                    LocalizationManager.Get(LocalizationKeys.BGREMOVAL_GO_TO_SETTINGS),
                    LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
                );

                if (goToSettings)
                {
                    OnNavigateToSettingsRequested?.Invoke();
                }
            }
        }

        private void CreateNoApiKeyWarning()
        {
            _noApiKeyWarning = new VisualElement();
            _noApiKeyWarning.AddToClassList("no-api-key-warning");

            Label warningLabel = new Label(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_NO_API_KEYS));
            warningLabel.AddToClassList("warning-label");

            Button goToSettingsButton = new Button(() => _providerSection?.ShowApiKeyInput());
            goToSettingsButton.text = LocalizationManager.Get(LocalizationKeys.BGREMOVAL_GO_TO_SETTINGS);
            goToSettingsButton.AddToClassList("go-to-settings-button");

            _noApiKeyWarning.Add(warningLabel);
            _noApiKeyWarning.Add(goToSettingsButton);

            Insert(0, _noApiKeyWarning);
        }

        private async void OnProcessClicked()
        {
            if (!HasEnabledProvider())
            {
                ShowFeatureDisabledWarning();
                return;
            }

            if (_inputSection == null || !_inputSection.HasInputImage())
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PLEASE_SELECT_IMAGE));
                return;
            }

            AttachedImageData inputImage = _inputSection.GetInputImage();
            if (inputImage == null)
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PLEASE_SELECT_IMAGE));
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            SetProcessing(true, LocalizationManager.Get(LocalizationKeys.BGREMOVAL_REMOVING));
            SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_REMOVING));

            BgRemovalRequestPayload requestPayload = new BgRemovalRequestPayload(inputImage.Base64Data, inputImage.MediaType);
            List<BgRemovalRequestProviderTarget> providerTargets = _providerSection != null
                ? _providerSection.BuildProviderTargets()
                : null;
            BgRemovalRequestParams removeRequestParams = new BgRemovalRequestParams
            {
                RequestPayload = requestPayload,
                Providers = providerTargets
            };

            try
            {
                BgRemovalResponse response = await _bgRemovalManager.RemoveBackgroundWithProvidersAsync(removeRequestParams, _cancellationTokenSource.Token);
                SetProcessing(false);
                string priorityLabel = BuildPriorityProviderLabel(response);

                // Get original texture to show before/after comparison
                Texture2D originalTexture = _inputSection?.GetInputImage()?.SourceTexture;
                _resultsSection?.AddResult(priorityLabel, response, originalTexture);

                SetStatus(response.IsSuccess
                    ? LocalizationManager.Get(LocalizationKeys.BGREMOVAL_SUCCESS)
                    : LocalizationManager.Get(LocalizationKeys.BGREMOVAL_FAILED, response.ErrorMessage));
            }
            catch (OperationCanceledException)
            {
                SetProcessing(false);
                SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_CANCELLED));
            }
            catch (Exception ex)
            {
                SetProcessing(false);
                SetStatus($"Error: {ex.Message}");
            }
        }

        private string BuildPriorityProviderLabel(BgRemovalResponse response_)
        {
            if (response_ == null)
            {
                return "Priority Provider";
            }

            BgRemovalProviderType providerType = response_ != null && response_.ProviderType != BgRemovalProviderType.NONE
                ? response_.ProviderType
                : _bgRemovalManager != null ? _bgRemovalManager.GetHighestPriorityProvider() : BgRemovalProviderType.NONE;

            string providerLabel = providerType != BgRemovalProviderType.NONE ? providerType.ToString() : string.Empty;
            string modelLabel = GetBgRemovalModelDisplayName(providerType, response_.Model);

            if (!string.IsNullOrEmpty(providerLabel) && !string.IsNullOrEmpty(modelLabel))
            {
                return $"{providerLabel} ({modelLabel})";
            }

            if (!string.IsNullOrEmpty(providerLabel))
            {
                return providerLabel;
            }

            if (!string.IsNullOrEmpty(modelLabel))
            {
                return modelLabel;
            }

            return "Priority Provider";
        }

        private string GetBgRemovalModelDisplayName(BgRemovalProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
            {
                return string.Empty;
            }

            if (providerType_ != BgRemovalProviderType.NONE)
            {
                BgRemovalModelInfo modelInfo = _manager != null
                    ? _manager.GetModelInfo(providerType_, modelId_)
                    : null;
                if (modelInfo != null && !string.IsNullOrEmpty(modelInfo.DisplayName))
                {
                    return modelInfo.DisplayName;
                }
            }

            return modelId_;
        }

        private async void OnProcessAllClicked()
        {
            if (!HasEnabledProvider())
            {
                ShowFeatureDisabledWarning();
                return;
            }

            if (_inputSection == null || !_inputSection.HasInputImage())
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PLEASE_SELECT_IMAGE));
                return;
            }

            AttachedImageData inputImage = _inputSection.GetInputImage();
            if (inputImage == null)
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PLEASE_SELECT_IMAGE));
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            SetProcessing(true, LocalizationManager.Get(LocalizationKeys.BGREMOVAL_REMOVING_ALL));
            SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_REMOVING_ALL));

            BgRemovalRequestPayload requestPayload = new BgRemovalRequestPayload(inputImage.Base64Data, inputImage.MediaType);
            List<BgRemovalRequestProviderTarget> providerTargets = _providerSection != null
                ? _providerSection.BuildProviderTargets()
                : null;
            BgRemovalRequestParams removeRequestParams = new BgRemovalRequestParams
            {
                RequestPayload = requestPayload,
                Providers = providerTargets
            };

            try
            {
                Dictionary<BgRemovalProviderType, BgRemovalResponse> responses =
                    await _bgRemovalManager.RemoveBackgroundToAllProvidersAsync(removeRequestParams, true, _cancellationTokenSource.Token);

                SetProcessing(false);

                // Get original texture to show before/after comparison
                Texture2D originalTexture = _inputSection?.GetInputImage()?.SourceTexture;

                foreach (KeyValuePair<BgRemovalProviderType, BgRemovalResponse> kvp in responses)
                {
                    _resultsSection?.AddResult(kvp.Key.ToString(), kvp.Value, originalTexture);
                }

                SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_RECEIVED_RESPONSES, responses.Count));
            }
            catch (OperationCanceledException)
            {
                SetProcessing(false);
                SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_CANCELLED));
            }
            catch (Exception ex)
            {
                SetProcessing(false);
                SetStatus($"Error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _bgRemovalManager?.Dispose();

            LocalizationManager.OnLanguageChanged -= UpdateSectionHeaders;

            _providerSection?.Dispose();
            _inputSection?.Dispose();
            _resultsSection?.Dispose();
        }
    }
}
