using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ImageView : VisualElement
    {
        private static readonly string UXML_PATH = EditorPaths.VIEWS_PATH + "Image/ImageView.uxml";
        private static readonly string USS_PATH = EditorPaths.VIEWS_PATH + "Image/ImageView.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnNavigateToSettingsRequested;

        private EditorDataStorage _storage;
        private ImageProviderManager _imageManager;
        private ImageAppProviderManager _imageAppManager;
        private EditorProviderManagerImage _editorProviderManager;
        private CancellationTokenSource _cancellationTokenSource;

        private VisualElement _noApiKeyWarning;
        private Label _noApiKeyWarningLabel;

        private ImageProviderSection _providerSection;
        private ImageRequestSection _requestSection;
        private ImageResultsSection _resultsSection;

        private bool _isGenerating;
        private string _currentLanguageCode;

        public ImageView(EditorDataStorage storage_)
        {
            _storage = storage_;
            _editorProviderManager = new EditorProviderManagerImage(_storage);
            LoadLayout();
            LoadStyles();
            LoadLanguageSetting();
            SetupUI();
            InitializeSections();
            InitializeManager();
            RefreshDynamicOptions();
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

        public void OnShow() { }

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

        private void SetupUI()
        {
            CreateNoApiKeyWarning();

            VisualElement providerContainer = this.Q<VisualElement>("provider-section-container");
            VisualElement requestContainer = this.Q<VisualElement>("request-section-container");
            VisualElement resultsContainer = this.Q<VisualElement>("results-section-container");

            _providerSection = new ImageProviderSection();
            _requestSection = new ImageRequestSection();
            _resultsSection = new ImageResultsSection();

            if (providerContainer != null)
            {
                providerContainer.Add(_providerSection);
            }
            else
            {
                Add(_providerSection);
            }

            if (requestContainer != null)
            {
                requestContainer.Add(_requestSection);
            }
            else
            {
                Add(_requestSection);
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
                _providerSection.OnActiveModelsChanged += RefreshDynamicOptions;
            }

            if (_requestSection != null)
            {
                _requestSection.OnStatusChanged += SetStatus;
                _requestSection.OnGenerateRequested += OnGenerateClicked;
                _requestSection.OnGenerateAllRequested += OnGenerateAllClicked;
                _requestSection.OnCancelRequested += OnCancelClicked;
                _requestSection.RegisterDisabledWarning(ShowFeatureDisabledWarning);
            }

            if (_resultsSection != null)
            {
                _resultsSection.OnStatusChanged += SetStatus;
            }
        }

        private void InitializeSections()
        {
            _providerSection?.Initialize(_storage, _imageManager, _imageAppManager, _editorProviderManager, _currentLanguageCode);
            if (_providerSection != null)
            {
                _providerSection.OnAuthChanged += HandleAuthChanged;
            }
            _requestSection?.Initialize(_editorProviderManager, _storage, _currentLanguageCode);
        }

        private void HandleAuthChanged()
        {
            InitializeManager();
            RefreshDynamicOptions();
            UpdateUIState();
        }

        private void UpdateSectionHeaders()
        {
            _currentLanguageCode = LocalizationManager.CurrentLanguageCode;
            _providerSection?.UpdateLanguage(_currentLanguageCode);
            _requestSection?.UpdateLocalization();
        }

        public void InitializeManager()
        {
            _imageManager?.Dispose();
            _imageAppManager?.Dispose();
            _imageManager = new ImageProviderManager();
            _imageAppManager = new ImageAppProviderManager();

            if (_storage == null)
                return;

            string openaiKey = _storage.GetString(EditorDataStorageKeys.KEY_OPENAI);
            string googleKey = _storage.GetString(EditorDataStorageKeys.KEY_GOOGLE);
            string openRouterKey = _storage.GetString(EditorDataStorageKeys.KEY_OPENROUTER);
            string codexApiKey = EditorDataStorageKeys.GetImageAppApiKey(_storage, ImageEditorProviderType.CODEX_APP);
            string codexExecutablePath = EditorDataStorageKeys.GetImageAppExecutablePath(_storage, ImageEditorProviderType.CODEX_APP);
            string codexNodeExecutablePath = EditorDataStorageKeys.GetImageAppNodeExecutablePath(_storage, ImageEditorProviderType.CODEX_APP);
            bool useCodexApiKey = EditorDataStorageKeys.GetImageAppUseApiKey(_storage, ImageEditorProviderType.CODEX_APP);

            if (!string.IsNullOrEmpty(openaiKey))
            {
                string model = _editorProviderManager != null ? _editorProviderManager.GetPrimarySelectedModelId(ImageEditorProviderType.OPEN_AI) : string.Empty;
                
                if (string.IsNullOrEmpty(model) && _editorProviderManager != null)
                    model = _editorProviderManager.GetDefaultImageModel(ImageEditorProviderType.OPEN_AI);
                
                ImageProviderSettings settings = new ImageProviderSettings(openaiKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                
                _imageManager.AddProvider(ImageProviderType.OPEN_AI, settings);
            }

            if (!string.IsNullOrEmpty(googleKey))
            {
                string geminiModel = _editorProviderManager != null ? _editorProviderManager.GetPrimarySelectedModelId(ImageEditorProviderType.GOOGLE_GEMINI)
                    : string.Empty;
                if (string.IsNullOrEmpty(geminiModel) && _editorProviderManager != null)
                    geminiModel = _editorProviderManager.GetDefaultImageModel(ImageEditorProviderType.GOOGLE_GEMINI);
                ImageProviderSettings geminiSettings = new ImageProviderSettings(googleKey);
                if (!string.IsNullOrEmpty(geminiModel))
                    geminiSettings.DefaultModel = geminiModel;
                _imageManager.AddProvider(ImageProviderType.GOOGLE_GEMINI, geminiSettings);

                string imagenModel = _editorProviderManager != null
                    ? _editorProviderManager.GetPrimarySelectedModelId(ImageEditorProviderType.GOOGLE_IMAGEN)
                    : string.Empty;
                if (string.IsNullOrEmpty(imagenModel) && _editorProviderManager != null)
                    imagenModel = _editorProviderManager.GetDefaultImageModel(ImageEditorProviderType.GOOGLE_IMAGEN);
                ImageProviderSettings imagenSettings = new ImageProviderSettings(googleKey);
                if (!string.IsNullOrEmpty(imagenModel))
                    imagenSettings.DefaultModel = imagenModel;
                _imageManager.AddProvider(ImageProviderType.GOOGLE_IMAGEN, imagenSettings);
            }

            if (!string.IsNullOrEmpty(openRouterKey))
            {
                string model = _editorProviderManager != null
                    ? _editorProviderManager.GetPrimarySelectedModelId(ImageEditorProviderType.OPEN_ROUTER)
                    : string.Empty;
                if (string.IsNullOrEmpty(model) && _editorProviderManager != null)
                    model = _editorProviderManager.GetDefaultImageModel(ImageEditorProviderType.OPEN_ROUTER);
                ImageProviderSettings settings = new ImageProviderSettings(openRouterKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                _imageManager.AddProvider(ImageProviderType.OPEN_ROUTER, settings);
            }

            if (!string.IsNullOrEmpty(codexExecutablePath))
            {
                string model = _editorProviderManager != null
                    ? _editorProviderManager.GetPrimarySelectedModelId(ImageEditorProviderType.CODEX_APP)
                    : string.Empty;
                if (string.IsNullOrEmpty(model) && _editorProviderManager != null)
                    model = _editorProviderManager.GetDefaultImageModel(ImageEditorProviderType.CODEX_APP);

                ImageAppProviderSettings settings = new ImageAppProviderSettings
                {
                    UseApiKey = useCodexApiKey,
                    AppExecutablePath = codexExecutablePath,
                    NodeExecutablePath = codexNodeExecutablePath,
                    ApiKey = useCodexApiKey ? codexApiKey : string.Empty,
                    DefaultModel = string.IsNullOrEmpty(model) ? Weppy.AIProvider.ImageModelPresets.CodexApp.CODEX_APP_IMAGE : model
                };
                _imageAppManager.AddProvider(ImageAppProviderType.CODEX_APP, settings);
            }

            _providerSection?.SetManager(_imageManager, _imageAppManager);
        }

        public void RefreshProviders()
        {
            LoadLanguageSetting();
            InitializeManager();
            _providerSection?.RefreshProviders();
            RefreshDynamicOptions();
            UpdateUIState();
        }

        private void RefreshDynamicOptions()
        {
            if (_providerSection == null || _requestSection == null)
                return;

            List<ImageModelInfo> activeModels = _providerSection.GetActiveModelInfos();
            _requestSection.RefreshDynamicOptions(activeModels);
        }

        private void SetGenerating(bool isGenerating_, string message_ = "Generating...")
        {
            _isGenerating = isGenerating_;
            _requestSection?.SetGenerating(isGenerating_, message_);
            UpdateUIState();
        }

        private void OnCancelClicked()
        {
            _cancellationTokenSource?.Cancel();
            SetGenerating(false);
            SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_CANCELLED));
        }

        private void UpdateUIState()
        {
            bool hasAnyKey = EditorDataStorageKeys.HasAnyImageProviderAuth(_storage);
            bool hasEnabledProvider = HasEnabledProvider();
            bool canExecute = hasEnabledProvider;

            if (_noApiKeyWarning != null)
            {
                _noApiKeyWarning.style.display = hasEnabledProvider ? DisplayStyle.None : DisplayStyle.Flex;
                if (_noApiKeyWarningLabel != null)
                {
                    _noApiKeyWarningLabel.text = GetDisabledReason();
                }
            }

            _requestSection?.SetActionButtonsEnabled(canExecute && !_isGenerating);
        }

        private bool HasEnabledProvider()
        {
            return _providerSection?.HasEnabledProvider() ?? false;
        }

        private string GetDisabledReason()
        {
            bool hasAnyKey = EditorDataStorageKeys.HasAnyImageProviderAuth(_storage);
            if (!hasAnyKey)
            {
                return LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NO_API_KEYS);
            }

            if (!HasEnabledProvider())
            {
                return LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NO_ENABLED_PROVIDER);
            }

            return string.Empty;
        }

        private void ShowFeatureDisabledWarning()
        {
            string reason = GetDisabledReason();
            if (string.IsNullOrEmpty(reason))
                return;

            SetStatus(reason);

            bool goToSettings = EditorUtility.DisplayDialog(
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_FEATURE_DISABLED_TITLE),
                reason,
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GO_TO_SETTINGS),
                LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
            );

            if (goToSettings)
            {
                OnNavigateToSettingsRequested?.Invoke();
            }
        }

        private ImageRequestPayload CreateImagePayload(string prompt_)
        {
            ImageEditorProviderType providerType = _providerSection != null
                ? _providerSection.GetHighestPriorityProvider()
                : ImageEditorProviderType.NONE;
            string model = "default";
            if (providerType != ImageEditorProviderType.NONE && _editorProviderManager != null)
            {
                model = _editorProviderManager.GetDefaultImageModel(providerType);
            }

            // Get model info to check supported options
            ImageModelInfo modelInfo = null;
            if (providerType != ImageEditorProviderType.NONE && _editorProviderManager != null && !string.IsNullOrEmpty(model))
            {
                modelInfo = _editorProviderManager.GetModelInfo(providerType, model);
            }

            ImageRequestPayload requestPayload = new ImageRequestPayload(prompt_, model);

            string negativePrompt = _requestSection?.GetNegativePrompt();
            if (!string.IsNullOrWhiteSpace(negativePrompt))
            {
                requestPayload.NegativePrompt = negativePrompt;
            }

            if (_requestSection != null)
            {
                Dictionary<string, string> selectedOptions = _requestSection.GetSelectedOptions();
                if (selectedOptions.Count > 0)
                {
                    List<string> unsupportedOptions = new List<string>();
                    requestPayload.AdditionalBodyParameters = new Dictionary<string, object>();

                    foreach (KeyValuePair<string, string> kvp in selectedOptions)
                    {
                        // Check if the model supports this option
                        bool isSupported = modelInfo != null && modelInfo.HasOption(kvp.Key);

                        if (isSupported)
                        {
                            ImageModelOptionDefinition optionDefinition = modelInfo.GetOption(kvp.Key);
                            string apiParameterName = optionDefinition != null && !string.IsNullOrEmpty(optionDefinition.apiParameterName)
                                ? optionDefinition.apiParameterName
                                : kvp.Key;
                            // Add to AdditionalBodyParameters if supported
                            requestPayload.AdditionalBodyParameters[apiParameterName] = kvp.Value;
                        }
                        else
                        {
                            // Add to unsupported options list
                            unsupportedOptions.Add($"{kvp.Key}: {kvp.Value}");
                        }
                    }

                    // Append unsupported options to prompt
                    if (unsupportedOptions.Count > 0)
                    {
                        string optionsText = string.Join(", ", unsupportedOptions);
                        requestPayload.Prompt = $"{prompt_}. {optionsText}";
                    }
                }

                IReadOnlyList<AttachedImageData> attachedImages = _requestSection.GetAttachedImages();
                if (attachedImages.Count > 0)
                {
                    requestPayload.InputImages = new List<ImageRequestInputData>();
                    foreach (AttachedImageData attachedImage in attachedImages)
                    {
                        requestPayload.InputImages.Add(new ImageRequestInputData(attachedImage.Base64Data, attachedImage.MediaType));
                    }
                }
            }

            return requestPayload;
        }

        private async Task<(ImageResponse response, ImageEditorProviderType providerType)> GenerateWithPriorityAsync(
            ImageRequestPayload requestPayload_,
            List<ImageEditorProviderTarget> providerTargets_,
            CancellationToken cancellationToken_)
        {
            if (providerTargets_ == null || providerTargets_.Count == 0)
                return (ImageResponse.FromError("No enabled providers available"), ImageEditorProviderType.NONE);

            string lastError = null;
            Exception lastException = null;

            foreach (ImageEditorProviderTarget target in providerTargets_)
            {
                try
                {
                    if (ImageEditorProviderTypeUtility.IsApiProvider(target.ProviderType))
                    {
                        if (_imageManager == null)
                            continue;

                        ImageProviderType apiProviderType = ImageEditorProviderTypeUtility.ToApiProviderType(target.ProviderType);
                        ImageRequestParams apiParams = BuildApiRequestParams(requestPayload_, apiProviderType, target.Model, target.Priority);
                        ImageResponse response = await _imageManager.GenerateImageWithProvidersAsync(apiParams, cancellationToken_);
                        if (response.IsSuccess)
                            return (response, target.ProviderType);

                        lastError = response.ErrorMessage;
                    }
                    else if (ImageEditorProviderTypeUtility.IsAppProvider(target.ProviderType))
                    {
                        if (_imageAppManager == null)
                            continue;

                        ImageAppProviderType appProviderType = ImageEditorProviderTypeUtility.ToAppProviderType(target.ProviderType);
                        ImageAppRequestParams appParams = BuildAppRequestParams(requestPayload_, appProviderType, target.Model, target.Priority);
                        ImageResponse response = await _imageAppManager.GenerateImageWithProvidersAsync(appParams, cancellationToken_);
                        if (response.IsSuccess)
                            return (response, target.ProviderType);

                        lastError = response.ErrorMessage;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    lastError = ex.Message;
                }
            }

            return (ImageResponse.FromError(lastError ?? lastException?.Message ?? "All providers failed"), ImageEditorProviderType.NONE);
        }

        private async Task GenerateToAllProvidersAsync(
            ImageRequestPayload requestPayload_,
            List<ImageEditorProviderTarget> providerTargets_,
            Action<ImageEditorProviderType, ImageResponse> onProviderCompleted_,
            CancellationToken cancellationToken_)
        {
            if (providerTargets_ == null || providerTargets_.Count == 0)
                return;

            SynchronizationContext mainThread = SynchronizationContext.Current;
            List<ImageRequestProviderTarget> apiTargets = BuildApiTargets(providerTargets_);
            List<ImageAppRequestProviderTarget> appTargets = BuildAppTargets(providerTargets_);

            List<Task> tasks = new List<Task>();

            if (apiTargets.Count > 0 && _imageManager != null)
            {
                ImageRequestParams apiParams = new ImageRequestParams
                {
                    RequestPayload = requestPayload_,
                    Providers = apiTargets
                };

                tasks.Add(_imageManager.GenerateImageToAllProvidersAsync(
                    apiParams,
                    (providerType_, response_) =>
                    {
                        ImageEditorProviderType editorType = ImageEditorProviderTypeUtility.FromApiProviderType(providerType_);
                        if (editorType != ImageEditorProviderType.NONE)
                            mainThread?.Post(_ => onProviderCompleted_?.Invoke(editorType, response_), null);
                    },
                    cancellationToken_));
            }

            if (appTargets.Count > 0 && _imageAppManager != null)
            {
                ImageAppRequestParams appParams = new ImageAppRequestParams
                {
                    RequestPayload = requestPayload_,
                    Providers = appTargets
                };

                tasks.Add(_imageAppManager.GenerateImageToAllProvidersAsync(
                    appParams,
                    (providerType_, response_) =>
                    {
                        ImageEditorProviderType editorType = ImageEditorProviderTypeUtility.FromAppProviderType(providerType_);
                        if (editorType != ImageEditorProviderType.NONE)
                            mainThread?.Post(_ => onProviderCompleted_?.Invoke(editorType, response_), null);
                    },
                    cancellationToken_));
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        private ImageRequestParams BuildApiRequestParams(
            ImageRequestPayload requestPayload_,
            ImageProviderType providerType_,
            string model_,
            int priority_)
        {
            List<ImageRequestProviderTarget> targets = new List<ImageRequestProviderTarget>
            {
                new ImageRequestProviderTarget
                {
                    ProviderType = providerType_,
                    Model = model_,
                    Priority = priority_
                }
            };

            return new ImageRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = targets
            };
        }

        private ImageAppRequestParams BuildAppRequestParams(
            ImageRequestPayload requestPayload_,
            ImageAppProviderType providerType_,
            string model_,
            int priority_)
        {
            List<ImageAppRequestProviderTarget> targets = new List<ImageAppRequestProviderTarget>
            {
                new ImageAppRequestProviderTarget
                {
                    ProviderType = providerType_,
                    Model = model_,
                    Priority = priority_
                }
            };

            return new ImageAppRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = targets
            };
        }

        private List<ImageRequestProviderTarget> BuildApiTargets(List<ImageEditorProviderTarget> providerTargets_)
        {
            List<ImageRequestProviderTarget> targets = new List<ImageRequestProviderTarget>();
            if (providerTargets_ == null)
                return targets;

            foreach (ImageEditorProviderTarget target in providerTargets_)
            {
                if (!ImageEditorProviderTypeUtility.IsApiProvider(target.ProviderType))
                    continue;

                targets.Add(new ImageRequestProviderTarget
                {
                    ProviderType = ImageEditorProviderTypeUtility.ToApiProviderType(target.ProviderType),
                    Model = target.Model,
                    Priority = target.Priority
                });
            }

            return targets;
        }

        private List<ImageAppRequestProviderTarget> BuildAppTargets(List<ImageEditorProviderTarget> providerTargets_)
        {
            List<ImageAppRequestProviderTarget> targets = new List<ImageAppRequestProviderTarget>();
            if (providerTargets_ == null)
                return targets;

            foreach (ImageEditorProviderTarget target in providerTargets_)
            {
                if (!ImageEditorProviderTypeUtility.IsAppProvider(target.ProviderType))
                    continue;

                targets.Add(new ImageAppRequestProviderTarget
                {
                    ProviderType = ImageEditorProviderTypeUtility.ToAppProviderType(target.ProviderType),
                    Model = target.Model,
                    Priority = target.Priority
                });
            }

            return targets;
        }

        private async void OnGenerateClicked()
        {
            if (!HasEnabledProvider())
            {
                ShowFeatureDisabledWarning();
                return;
            }

            string prompt = _requestSection?.GetPrompt() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_PLEASE_ENTER_PROMPT));
                EditorUtility.DisplayDialog(
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_FEATURE_DISABLED_TITLE),
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_PLEASE_ENTER_PROMPT),
                    "OK"
                );
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // Create placeholder for priority provider
            List<ImageEditorProviderTarget> providerTargets = _providerSection != null
                ? _providerSection.BuildProviderTargets()
                : null;

            if (providerTargets != null && providerTargets.Count > 0)
            {
                ImageEditorProviderTarget priorityTarget = providerTargets[0]; // Highest priority
                _resultsSection?.CreatePlaceholder(priorityTarget.ProviderType, priorityTarget.Model);
            }

            SetGenerating(true, LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GENERATING_REQUEST));
            SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GENERATING_REQUEST));

            try
            {
                ImageRequestPayload requestPayload = CreateImagePayload(prompt);

                (ImageResponse response, ImageEditorProviderType providerType) result =
                    await GenerateWithPriorityAsync(requestPayload, providerTargets, _cancellationTokenSource.Token);

                SetGenerating(false);
                string priorityLabel = BuildPriorityProviderLabel(result.providerType, result.response);
                _resultsSection?.AddResult(priorityLabel, result.response);
                SetStatus(result.response.IsSuccess
                              ? LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GENERATED)
                              : LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_FAILED, result.response.ErrorMessage));
            }
            catch (OperationCanceledException)
            {
                SetGenerating(false);
                _resultsSection?.ClearPlaceholders();
                SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_CANCELLED));
            }
            catch (Exception ex)
            {
                SetGenerating(false);
                SetStatus($"Error: {ex.Message}");
            }
        }

        private string BuildPriorityProviderLabel(ImageEditorProviderType providerType_, ImageResponse response_)
        {
            string returnString = LocalizationManager.Get(LocalizationKeys.PROVIDER_PRIORITY_PROVIDER);
            if (response_ == null)
            {
                return returnString;
            }

            ImageEditorProviderType resolvedProviderType = providerType_ != ImageEditorProviderType.NONE
                ? providerType_
                : response_.ProviderType != ImageProviderType.NONE
                    ? ImageEditorProviderTypeUtility.FromApiProviderType(response_.ProviderType)
                    : _providerSection != null
                        ? _providerSection.GetHighestPriorityProvider()
                        : ImageEditorProviderType.NONE;

            string providerLabel = resolvedProviderType != ImageEditorProviderType.NONE ? resolvedProviderType.ToString() : string.Empty;
            string modelLabel = GetImageModelDisplayName(resolvedProviderType, response_.Model);

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

        private string GetImageModelDisplayName(ImageEditorProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
            {
                return string.Empty;
            }

            if (providerType_ != ImageEditorProviderType.NONE)
            {
                ImageModelInfo modelInfo = _editorProviderManager != null
                    ? _editorProviderManager.GetModelInfo(providerType_, modelId_)
                    : null;
                if (modelInfo != null && !string.IsNullOrEmpty(modelInfo.DisplayName))
                {
                    return modelInfo.DisplayName;
                }
            }

            return modelId_;
        }

        private async void OnGenerateAllClicked()
        {
            if (!HasEnabledProvider())
            {
                ShowFeatureDisabledWarning();
                return;
            }

            string prompt = _requestSection?.GetPrompt() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_PLEASE_ENTER_PROMPT));
                EditorUtility.DisplayDialog(
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_FEATURE_DISABLED_TITLE),
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_PLEASE_ENTER_PROMPT),
                    "OK"
                );
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // Create placeholders for all enabled providers
            List<ImageEditorProviderTarget> providerTargets = _providerSection != null
                ? _providerSection.BuildProviderTargets()
                : null;

            if (providerTargets != null)
            {
                foreach (ImageEditorProviderTarget target in providerTargets)
                {
                    _resultsSection?.CreatePlaceholder(target.ProviderType, target.Model);
                }
            }

            SetGenerating(true, LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GENERATING_ALL));
            SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GENERATING_ALL));

            try
            {
                ImageRequestPayload requestPayload = CreateImagePayload(prompt);

                int completedCount = 0;
                int totalCount = providerTargets?.Count ?? 0;

                await GenerateToAllProvidersAsync(
                    requestPayload,
                    providerTargets,
                    (providerType_, response_) =>
                    {
                        _resultsSection?.AddResult(providerType_.ToString(), response_);
                        completedCount++;
                        SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_RECEIVED_RESPONSES, completedCount));
                    },
                    _cancellationTokenSource.Token);

                SetGenerating(false);
                SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_RECEIVED_RESPONSES, completedCount));
            }
            catch (OperationCanceledException)
            {
                SetGenerating(false);
                _resultsSection?.ClearPlaceholders();
                SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_CANCELLED));
            }
            catch (Exception ex)
            {
                SetGenerating(false);
                SetStatus($"Error: {ex.Message}");
            }
        }

        private void CreateNoApiKeyWarning()
        {
            _noApiKeyWarning = new VisualElement();
            _noApiKeyWarning.AddToClassList("no-api-key-warning");

            _noApiKeyWarningLabel = new Label(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NO_API_KEYS));
            _noApiKeyWarningLabel.AddToClassList("warning-label");

            Button goToSettingsButton = new Button(() => _providerSection?.ShowApiKeyInput());
            goToSettingsButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GO_TO_SETTINGS);
            goToSettingsButton.AddToClassList("go-to-settings-button");

            _noApiKeyWarning.Add(_noApiKeyWarningLabel);
            _noApiKeyWarning.Add(goToSettingsButton);

            Insert(0, _noApiKeyWarning);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _imageManager?.Dispose();
            _imageAppManager?.Dispose();

            LocalizationManager.OnLanguageChanged -= UpdateSectionHeaders;

            _providerSection?.Dispose();
            _requestSection?.Dispose();
            _resultsSection?.Dispose();
        }
    }
}
