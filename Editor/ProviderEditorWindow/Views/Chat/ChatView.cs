using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ChatView : VisualElement
    {
        private static readonly string UXML_PATH = EditorPaths.VIEWS_PATH + "Chat/ChatView.uxml";
        private static readonly string USS_PATH = EditorPaths.VIEWS_PATH + "Chat/ChatView.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnNavigateToSettingsRequested;

        private EditorDataStorage _storage;
        private ChatProviderManager _chatManager;
        private ChatCliProviderManager _cliManager;
        private EditorProviderManagerChat _manager;
        private CancellationTokenSource _cancellationTokenSource;

        private string _currentLanguageCode;

        private VisualElement _noApiKeyWarning;
        private Label _warningLabelRef;
        private Button _goToSettingsButtonRef;

        private ChatProviderSection _chatProviderSection;
        private ConversationSection _conversationSection;
        private ChatInputSection _chatInputSection;

        public ChatView(EditorDataStorage storage_)
        {
            _storage = storage_;
            _manager = new EditorProviderManagerChat(_storage);
            LoadLayout();
            LoadStyles();
            SetupUI();
            LoadLanguageSetting();
            InitializeManager();
            InitializeSections();
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
            _chatProviderSection?.OnHide();
        }

        public void SaveHistoryIfNeeded()
        {
            _conversationSection?.SaveHistoryIfNeeded();
        }

        private void LoadLanguageSetting()
        {
            _currentLanguageCode = LocalizationManager.CurrentLanguageCode;
        }

        private void InitializeSections()
        {
            _chatProviderSection?.Initialize(_storage, _manager, _currentLanguageCode);
            if (_chatProviderSection != null)
            {
                _chatProviderSection.OnAuthChanged += HandleAuthChanged;
            }
            _chatInputSection?.Initialize(HasEnabledProvider, _storage);
            UpdateUIState();
        }

        private void HandleAuthChanged()
        {
            InitializeManager();
            UpdateUIState();
        }

        private void SetupUI()
        {
            CreateNoApiKeyWarning();

            VisualElement modelContainer = this.Q<VisualElement>("model-section-container");
            VisualElement conversationContainer = this.Q<VisualElement>("conversation-section-container");
            VisualElement inputContainer = this.Q<VisualElement>("input-section-container");

            _chatProviderSection = new ChatProviderSection();
            _conversationSection = new ConversationSection();
            _chatInputSection = new ChatInputSection();

            if (modelContainer != null)
            {
                modelContainer.Add(_chatProviderSection);
            }
            else
            {
                Insert(1, _chatProviderSection);
            }

            if (conversationContainer != null)
            {
                conversationContainer.Add(_conversationSection);
            }
            else
            {
                Add(_conversationSection);
            }

            if (inputContainer != null)
            {
                inputContainer.Add(_chatInputSection);
            }
            else
            {
                Add(_chatInputSection);
            }

            WireModelSelectionEvents();
            WireConversationEvents();
            WireChatInputEvents();

            LocalizationManager.OnLanguageChanged += UpdateSectionHeaders;
        }

        private void WireModelSelectionEvents()
        {
            _chatProviderSection.OnStatusChanged += SetStatus;
            _chatProviderSection.OnNavigateToSettingsRequested += () => OnNavigateToSettingsRequested?.Invoke();
            _chatProviderSection.OnProviderOrderChanged += HandleProviderOrderChanged;
            _chatProviderSection.OnProviderEnabledChanged += HandleProviderEnabledChanged;
            _chatProviderSection.OnModelChanged += HandleModelChanged;
        }

        private void WireConversationEvents()
        {
            _conversationSection.OnStatusChanged += SetStatus;
            _conversationSection.OnClearRequested += () => SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_MESSAGES_CLEARED));
        }

        private void WireChatInputEvents()
        {
            _chatInputSection.OnStatusChanged += SetStatus;
            _chatInputSection.OnNavigateToSettingsRequested += () => OnNavigateToSettingsRequested?.Invoke();
            _chatInputSection.OnSendRequested += HandleSendRequested;
            _chatInputSection.OnResetSessionRequested += HandleResetSessionRequested;
            _chatInputSection.RegisterCancelCallback(OnCancelClicked);
        }

        private void HandleProviderOrderChanged()
        {
            InitializeManager();
        }

        private void HandleProviderEnabledChanged(ChatEditorProviderType providerType_, bool enabled_)
        {
            if (ChatEditorProviderTypeUtility.IsApiProvider(providerType_))
            {
                ChatProviderType apiProviderType = ChatEditorProviderTypeUtility.ToApiProviderType(providerType_);
                _chatManager?.SetProviderEnabled(apiProviderType, enabled_);
            }
            else if (ChatEditorProviderTypeUtility.IsCliProvider(providerType_))
            {
                ChatCliProviderType cliProviderType = ChatEditorProviderTypeUtility.ToCliProviderType(providerType_);
                _cliManager?.SetProviderEnabled(cliProviderType, enabled_);
            }

            UpdateUIState();
        }

        private void HandleModelChanged(ChatEditorProviderType providerType_, string modelId_)
        {
            if (ChatEditorProviderTypeUtility.IsApiProvider(providerType_))
            {
                ChatProviderType apiProviderType = ChatEditorProviderTypeUtility.ToApiProviderType(providerType_);
                _chatManager?.SetProviderDefaultModel(apiProviderType, modelId_);
            }
            else if (ChatEditorProviderTypeUtility.IsCliProvider(providerType_))
            {
                ChatCliProviderType cliProviderType = ChatEditorProviderTypeUtility.ToCliProviderType(providerType_);
                _cliManager?.SetProviderDefaultModel(cliProviderType, modelId_);
            }
        }

        private void CreateNoApiKeyWarning()
        {
            _noApiKeyWarning = new VisualElement();
            _noApiKeyWarning.AddToClassList("no-api-key-warning");

            _warningLabelRef = new Label(LocalizationManager.Get(LocalizationKeys.CHAT_NO_API_KEYS));
            _warningLabelRef.AddToClassList("warning-label");

            _goToSettingsButtonRef = new Button(() => _chatProviderSection?.ShowApiKeyInput());
            _goToSettingsButtonRef.text = LocalizationManager.Get(LocalizationKeys.CHAT_GO_TO_SETTINGS);
            _goToSettingsButtonRef.AddToClassList("go-to-settings-button");

            _noApiKeyWarning.Add(_warningLabelRef);
            _noApiKeyWarning.Add(_goToSettingsButtonRef);

            Insert(0, _noApiKeyWarning);
        }

        private void UpdateSectionHeaders()
        {
            _currentLanguageCode = LocalizationManager.CurrentLanguageCode;
            _chatProviderSection?.UpdateLanguage(_currentLanguageCode);
            _chatInputSection?.UpdateLocalization();
        }

        public void InitializeManager()
        {
            _chatManager?.Dispose();
            _cliManager?.Dispose();
            _chatManager = new ChatProviderManager();
            _cliManager = new ChatCliProviderManager();

            if (_storage == null)
                return;

            string openaiKey = _storage.GetString(EditorDataStorageKeys.KEY_OPENAI);
            string googleKey = _storage.GetString(EditorDataStorageKeys.KEY_GOOGLE);
            string anthropicKey = _storage.GetString(EditorDataStorageKeys.KEY_ANTHROPIC);
            string huggingfaceKey = _storage.GetString(EditorDataStorageKeys.KEY_HUGGINGFACE);
            string openRouterKey = _storage.GetString(EditorDataStorageKeys.KEY_OPENROUTER);

            if (!string.IsNullOrEmpty(openaiKey))
            {
                string model = _chatProviderSection?.GetPrimarySelectedModel(ChatEditorProviderType.OPEN_AI) ?? string.Empty;
                if (string.IsNullOrEmpty(model) && _manager != null)
                    model = _manager.GetDefaultChatModel(ChatEditorProviderType.OPEN_AI);
                
                ChatProviderSettings settings = new ChatProviderSettings(openaiKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                
                _chatManager.AddProvider(ChatProviderType.OPEN_AI, settings);
            }
            if (!string.IsNullOrEmpty(googleKey))
            {
                string model = _chatProviderSection?.GetPrimarySelectedModel(ChatEditorProviderType.GOOGLE) ?? string.Empty;
                if (string.IsNullOrEmpty(model) && _manager != null)
                    model = _manager.GetDefaultChatModel(ChatEditorProviderType.GOOGLE);
                
                ChatProviderSettings settings = new ChatProviderSettings(googleKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                
                _chatManager.AddProvider(ChatProviderType.GOOGLE, settings);
            }
            if (!string.IsNullOrEmpty(anthropicKey))
            {
                string model = _chatProviderSection?.GetPrimarySelectedModel(ChatEditorProviderType.ANTHROPIC) ?? string.Empty;
                if (string.IsNullOrEmpty(model) && _manager != null)
                    model = _manager.GetDefaultChatModel(ChatEditorProviderType.ANTHROPIC);
                
                ChatProviderSettings settings = new ChatProviderSettings(anthropicKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                
                _chatManager.AddProvider(ChatProviderType.ANTHROPIC, settings);
            }
            if (!string.IsNullOrEmpty(huggingfaceKey))
            {
                string model = _chatProviderSection?.GetPrimarySelectedModel(ChatEditorProviderType.HUGGING_FACE) ?? string.Empty;
                if (string.IsNullOrEmpty(model) && _manager != null)
                    model = _manager.GetDefaultChatModel(ChatEditorProviderType.HUGGING_FACE);
                
                ChatProviderSettings settings = new ChatProviderSettings(huggingfaceKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                
                _chatManager.AddProvider(ChatProviderType.HUGGING_FACE, settings);
            }

            if (!string.IsNullOrEmpty(openRouterKey))
            {
                string model = _chatProviderSection?.GetPrimarySelectedModel(ChatEditorProviderType.OPEN_ROUTER) ?? string.Empty;
                if (string.IsNullOrEmpty(model) && _manager != null)
                    model = _manager.GetDefaultChatModel(ChatEditorProviderType.OPEN_ROUTER);
                
                ChatProviderSettings settings = new ChatProviderSettings(openRouterKey);
                if (!string.IsNullOrEmpty(model))
                    settings.DefaultModel = model;
                
                _chatManager.AddProvider(ChatProviderType.OPEN_ROUTER, settings);
            }

            bool useCliApiKey = EditorDataStorageKeys.GetCliUseApiKey(_storage, ChatEditorProviderType.CODEX_CLI);
            string cliApiKey = EditorDataStorageKeys.GetCliApiKey(_storage, ChatEditorProviderType.CODEX_CLI);
            string codexCliExecutablePath = EditorDataStorageKeys.GetCliExecutablePath(_storage, ChatEditorProviderType.CODEX_CLI);
            string nodeExecutablePath = EditorDataStorageKeys.GetNodeExecutablePath(_storage, ChatEditorProviderType.CODEX_CLI);
            ChatCliProviderSettings codexCliSettings = new ChatCliProviderSettings(cliApiKey)
            {
                UseApiKey = useCliApiKey,
            };
            if (!string.IsNullOrEmpty(codexCliExecutablePath))
                codexCliSettings.CliExecutablePath = codexCliExecutablePath;
            
            if (!string.IsNullOrEmpty(nodeExecutablePath))
                codexCliSettings.NodeExecutablePath = nodeExecutablePath;

            string cliModel = _manager != null
                ? _manager.GetPrimarySelectedModelId(ChatEditorProviderType.CODEX_CLI)
                : string.Empty;

            if (string.IsNullOrEmpty(cliModel) && _manager != null)
                cliModel = _manager.GetDefaultChatModel(ChatEditorProviderType.CODEX_CLI);

            if (!string.IsNullOrEmpty(cliModel))
                codexCliSettings.DefaultModel = cliModel;

            _cliManager.AddProvider(ChatCliProviderType.CODEX_CLI, codexCliSettings);

            string claudeCodeApiKey = EditorDataStorageKeys.GetCliApiKey(_storage, ChatEditorProviderType.CLAUDE_CODE_CLI);
            bool useClaudeCodeApiKey = EditorDataStorageKeys.GetCliUseApiKey(_storage, ChatEditorProviderType.CLAUDE_CODE_CLI);
            string claudeCodeCliPath = EditorDataStorageKeys.GetCliExecutablePath(_storage, ChatEditorProviderType.CLAUDE_CODE_CLI);

            ChatCliProviderSettings claudeCodeCliSettings = new ChatCliProviderSettings(claudeCodeApiKey)
            {
                UseApiKey = useClaudeCodeApiKey,
            };
            if (!string.IsNullOrEmpty(claudeCodeCliPath))
                claudeCodeCliSettings.CliExecutablePath = claudeCodeCliPath;

            string claudeCodeModel = _manager != null ? _manager.GetPrimarySelectedModelId(ChatEditorProviderType.CLAUDE_CODE_CLI) : string.Empty;

            if (string.IsNullOrEmpty(claudeCodeModel) && _manager != null)
                claudeCodeModel = _manager.GetDefaultChatModel(ChatEditorProviderType.CLAUDE_CODE_CLI);

            if (!string.IsNullOrEmpty(claudeCodeModel))
                claudeCodeCliSettings.DefaultModel = claudeCodeModel;

            _cliManager.AddProvider(ChatCliProviderType.CLAUDE_CODE_CLI, claudeCodeCliSettings);

            string geminiCliApiKey = EditorDataStorageKeys.GetCliApiKey(_storage, ChatEditorProviderType.GEMINI_CLI);
            bool useGeminiCliApiKey = EditorDataStorageKeys.GetCliUseApiKey(_storage, ChatEditorProviderType.GEMINI_CLI);
            string geminiCliPath = EditorDataStorageKeys.GetCliExecutablePath(_storage, ChatEditorProviderType.GEMINI_CLI);

            ChatCliProviderSettings geminiCliSettings = new ChatCliProviderSettings(geminiCliApiKey)
            {
                UseApiKey = useGeminiCliApiKey,
            };
            if (!string.IsNullOrEmpty(geminiCliPath))
                geminiCliSettings.CliExecutablePath = geminiCliPath;

            string geminiCliModel = _manager != null ? _manager.GetPrimarySelectedModelId(ChatEditorProviderType.GEMINI_CLI) : string.Empty;

            if (string.IsNullOrEmpty(geminiCliModel) && _manager != null)
                geminiCliModel = _manager.GetDefaultChatModel(ChatEditorProviderType.GEMINI_CLI);

            if (!string.IsNullOrEmpty(geminiCliModel))
                geminiCliSettings.DefaultModel = geminiCliModel;

            _cliManager.AddProvider(ChatCliProviderType.GEMINI_CLI, geminiCliSettings);
        }

        public void RefreshProviders()
        {
            LoadLanguageSetting();
            InitializeManager();
            _chatProviderSection?.RefreshProviders();
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool hasAnyKey = EditorDataStorageKeys.HasAnyChatProviderAuth(_storage);
            bool hasEnabledProvider = HasEnabledProvider();
            bool canExecute = hasAnyKey && hasEnabledProvider;

            if (_noApiKeyWarning != null)
                _noApiKeyWarning.style.display = hasAnyKey ? DisplayStyle.None : DisplayStyle.Flex;

            _chatInputSection?.SetInputEnabled(canExecute);
            _chatInputSection?.SetPersistentControlsVisible(HasCliProviderAvailable());
        }

        private bool HasCliModelOption()
        {
            if (_manager == null)
                return false;

            IReadOnlyList<string> selectedModels = _manager.GetSelectedModelIds(ChatEditorProviderType.CODEX_CLI);
            return selectedModels != null && selectedModels.Count > 0;
        }

        private bool HasEnabledProvider()
        {
            return _chatProviderSection?.HasEnabledProvider() ?? false;
        }

        private void SetSending(bool isSending_, string message_ = "Sending...")
        {
            _chatInputSection?.SetSending(isSending_, message_);
        }

        private void OnCancelClicked()
        {
            _cancellationTokenSource?.Cancel();
            SetSending(false);
            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_CANCELLED));
        }

        private bool HasCliProviderAvailable()
        {
            List<ChatEditorProviderTarget> targets = _chatProviderSection?.BuildProviderTargets();
            if (targets == null)
                return false;

            foreach (ChatEditorProviderTarget target in targets)
            {
                if (ChatEditorProviderTypeUtility.IsCliProvider(target.ProviderType))
                    return true;
            }

            return false;
        }

        private void HandleResetSessionRequested()
        {
            _cliManager?.ResetAllSessions();
            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_CLI_SESSION_RESET));
        }

        private async void HandleSendRequested(ChatInputData inputData_)
        {
            if (!HasEnabledProvider())
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_NO_ENABLED_PROVIDER));
                return;
            }

            if (string.IsNullOrWhiteSpace(inputData_.UserPrompt))
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_PLEASE_ENTER_MESSAGE));
                return;
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            string sendingMessage = inputData_.SendToAll
                ? LocalizationManager.Get(LocalizationKeys.CHAT_SENDING_TO_ALL)
                : LocalizationManager.Get(LocalizationKeys.CHAT_SENDING_REQUEST);
            SetSending(true, sendingMessage);
            SetStatus(sendingMessage);

            _conversationSection?.AddUserMessage(inputData_.UserPrompt, inputData_.AttachedFiles?.Count ?? 0);

            ChatRequestPayload requestPayload = BuildChatPayload(inputData_);
            ChatCliRequestPayload cliPayload = BuildCliPayload(requestPayload);
            List<ChatEditorProviderTarget> providerTargets = _chatProviderSection != null
                ? _chatProviderSection.BuildProviderTargets()
                : null;

            try
            {
                if (inputData_.SendToAll)
                {
                    // Create placeholder entries for all providers
                    Dictionary<ChatEditorProviderType, VisualElement> messageEntries = new Dictionary<ChatEditorProviderType, VisualElement>();
                    if (providerTargets != null)
                    {
                        foreach (ChatEditorProviderTarget target in providerTargets)
                        {
                            VisualElement entry = _conversationSection?.CreateStreamingMessageEntry(
                                target.ProviderType.ToString(),
                                target.Model);
                            if (entry != null)
                            {
                                messageEntries[target.ProviderType] = entry;
                            }
                        }
                    }

                    int completedCount = 0;
                    await SendToAllProvidersAsync(requestPayload, cliPayload, providerTargets, inputData_.UsePersistent,
                        (ChatEditorProviderType providerType_, ChatResponse response_) =>
                        {
                            if (messageEntries.TryGetValue(providerType_, out VisualElement messageEntry))
                            {
                                if (response_.IsSuccess)
                                {
                                    _conversationSection?.UpdateStreamingContent(messageEntry, response_.Content);
                                    _conversationSection?.FinalizeStreamingEntry(messageEntry, true, LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS));
                                }
                                else
                                {
                                    _conversationSection?.UpdateStreamingContent(messageEntry, response_.ErrorMessage);
                                    _conversationSection?.FinalizeStreamingEntry(messageEntry, false, LocalizationManager.Get(LocalizationKeys.COMMON_ERROR));
                                }
                            }

                            completedCount++;
                            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_RECEIVED_RESPONSES, completedCount));
                        },
                        _cancellationTokenSource.Token);

                    SetSending(false);
                    _chatInputSection?.ClearAttachments();
                    SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_RECEIVED_RESPONSES, messageEntries.Count));
                }
                else
                {
                    bool usePersistent = inputData_.UsePersistent && HasCliTargetInPriority(providerTargets);

                    if (usePersistent)
                    {
                        await SendWithPersistentPriorityAsync(requestPayload, cliPayload, providerTargets);
                    }
                    else if (inputData_.UseStreaming && CanStreamWithPriority(providerTargets))
                    {
                        ChatRequestParams streamParams = BuildApiRequestParams(requestPayload, BuildApiTargets(providerTargets));
                        await SendWithStreamingAsync(streamParams);
                    }
                    else
                    {
                        // Create placeholder message entry with provider and model info
                        string providerName = LocalizationManager.Get(LocalizationKeys.PROVIDER_PRIORITY_PROVIDER);
                        string modelName = null;

                        if (providerTargets != null && providerTargets.Count > 0)
                        {
                            ChatEditorProviderTarget firstTarget = providerTargets[0];
                            providerName = firstTarget.ProviderType.ToString();
                            modelName = firstTarget.Model;
                        }

                        VisualElement messageEntry = _conversationSection?.CreateStreamingMessageEntry(providerName, modelName);

                        (ChatResponse response, ChatEditorProviderType providerType) result =
                            await SendWithPriorityAsync(requestPayload, cliPayload, providerTargets, _cancellationTokenSource.Token);

                        SetSending(false);

                        // Update the message entry with response content
                        if (result.response.IsSuccess)
                        {
                            _conversationSection?.UpdateStreamingContent(messageEntry, result.response.Content);
                            _conversationSection?.FinalizeStreamingEntry(messageEntry, true, LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS));
                            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_COMPLETED));
                            _chatInputSection?.ClearAttachments();
                        }
                        else
                        {
                            _conversationSection?.UpdateStreamingContent(messageEntry, result.response.ErrorMessage);
                            _conversationSection?.FinalizeStreamingEntry(messageEntry, false, LocalizationManager.Get(LocalizationKeys.COMMON_ERROR));
                            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_FAILED, result.response.ErrorMessage));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SetSending(false);
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_CANCELLED));
            }
            catch (Exception ex)
            {
                SetSending(false);
                SetStatus($"Error: {ex.Message}");
            }
        }

        private bool CanStreamWithPriority(List<ChatEditorProviderTarget> providerTargets_)
        {
            if (providerTargets_ == null || providerTargets_.Count == 0)
                return false;

            return ChatEditorProviderTypeUtility.IsApiProvider(providerTargets_[0].ProviderType);
        }

        private bool HasCliTargetInPriority(List<ChatEditorProviderTarget> providerTargets_)
        {
            if (providerTargets_ == null || providerTargets_.Count == 0)
                return false;

            return ChatEditorProviderTypeUtility.IsCliProvider(providerTargets_[0].ProviderType);
        }

        private async System.Threading.Tasks.Task SendWithPersistentPriorityAsync(
            ChatRequestPayload requestPayload_,
            ChatCliRequestPayload cliPayload_,
            List<ChatEditorProviderTarget> providerTargets_)
        {
            if (providerTargets_ == null || providerTargets_.Count == 0)
                return;

            string providerName = providerTargets_[0].ProviderType.ToString();
            string modelName = providerTargets_[0].Model;
            VisualElement messageEntry = _conversationSection?.CreateStreamingMessageEntry(providerName, modelName);

            string lastError = null;

            foreach (ChatEditorProviderTarget target in providerTargets_)
            {
                try
                {
                    if (ChatEditorProviderTypeUtility.IsCliProvider(target.ProviderType))
                    {
                        if (_cliManager == null)
                            continue;

                        ChatCliProviderType cliProviderType = ChatEditorProviderTypeUtility.ToCliProviderType(target.ProviderType);
                        ChatCliRequestParams cliParams = BuildCliRequestParams(cliPayload_, cliProviderType, target.Model, target.Priority);
                        ChatCliResponse cliResponse = await _cliManager.SendPersistentMessageWithProvidersAsync(cliParams, _cancellationTokenSource.Token);
                        ChatResponse response = MapCliResponse(cliResponse);

                        if (response.IsSuccess)
                        {
                            SetSending(false);
                            _conversationSection?.UpdateStreamingContent(messageEntry, response.Content);
                            _conversationSection?.FinalizeStreamingEntry(messageEntry, true, LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS));
                            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_COMPLETED));
                            _chatInputSection?.ClearAttachments();
                            return;
                        }

                        lastError = response.ErrorMessage;
                    }
                    else if (ChatEditorProviderTypeUtility.IsApiProvider(target.ProviderType))
                    {
                        if (_chatManager == null)
                            continue;

                        ChatProviderType apiProviderType = ChatEditorProviderTypeUtility.ToApiProviderType(target.ProviderType);
                        ChatRequestParams apiParams = BuildApiRequestParams(requestPayload_, apiProviderType, target.Model, target.Priority);
                        ChatResponse response = await _chatManager.SendMessageWithProvidersAsync(apiParams, _cancellationTokenSource.Token);

                        if (response.IsSuccess)
                        {
                            SetSending(false);
                            _conversationSection?.UpdateStreamingContent(messageEntry, response.Content);
                            _conversationSection?.FinalizeStreamingEntry(messageEntry, true, LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS));
                            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_COMPLETED));
                            _chatInputSection?.ClearAttachments();
                            return;
                        }

                        lastError = response.ErrorMessage;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
            }

            SetSending(false);
            _conversationSection?.UpdateStreamingContent(messageEntry, lastError ?? "All providers failed");
            _conversationSection?.FinalizeStreamingEntry(messageEntry, false, LocalizationManager.Get(LocalizationKeys.COMMON_ERROR));
            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_FAILED, lastError ?? "All providers failed"));
        }

        private async System.Threading.Tasks.Task<(ChatResponse response, ChatEditorProviderType providerType)> SendWithPriorityAsync(
            ChatRequestPayload requestPayload_,
            ChatCliRequestPayload cliPayload_,
            List<ChatEditorProviderTarget> providerTargets_,
            CancellationToken cancellationToken_)
        {
            if (providerTargets_ == null || providerTargets_.Count == 0)
                return (ChatResponse.FromError("No enabled providers available"), ChatEditorProviderType.NONE);

            string lastError = null;
            Exception lastException = null;

            foreach (ChatEditorProviderTarget target in providerTargets_)
            {
                try
                {
                    if (ChatEditorProviderTypeUtility.IsApiProvider(target.ProviderType))
                    {
                        if (_chatManager == null)
                            continue;

                        ChatProviderType apiProviderType = ChatEditorProviderTypeUtility.ToApiProviderType(target.ProviderType);
                        ChatRequestParams apiParams = BuildApiRequestParams(requestPayload_, apiProviderType, target.Model, target.Priority);
                        ChatResponse response = await _chatManager.SendMessageWithProvidersAsync(apiParams, cancellationToken_);
                        if (response.IsSuccess)
                            return (response, target.ProviderType);

                        lastError = response.ErrorMessage;
                    }
                    else if (ChatEditorProviderTypeUtility.IsCliProvider(target.ProviderType))
                    {
                        if (_cliManager == null)
                            continue;

                        ChatCliProviderType cliProviderType = ChatEditorProviderTypeUtility.ToCliProviderType(target.ProviderType);
                        ChatCliRequestParams cliParams = BuildCliRequestParams(cliPayload_, cliProviderType, target.Model, target.Priority);
                        ChatCliResponse cliResponse = await _cliManager.SendMessageWithProvidersAsync(cliParams, cancellationToken_);
                        ChatResponse response = MapCliResponse(cliResponse);
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

            return (ChatResponse.FromError(lastError ?? lastException?.Message ?? "All providers failed"), ChatEditorProviderType.NONE);
        }

        private async System.Threading.Tasks.Task SendToAllProvidersAsync(
            ChatRequestPayload requestPayload_,
            ChatCliRequestPayload cliPayload_,
            List<ChatEditorProviderTarget> providerTargets_,
            bool usePersistent_,
            Action<ChatEditorProviderType, ChatResponse> onProviderCompleted_,
            CancellationToken cancellationToken_)
        {
            if (providerTargets_ == null || providerTargets_.Count == 0)
                return;

            SynchronizationContext mainThread = SynchronizationContext.Current;

            List<ChatRequestProviderTarget> apiTargets = BuildApiTargets(providerTargets_);
            List<ChatCliRequestProviderTarget> cliTargets = BuildCliTargets(providerTargets_);

            List<System.Threading.Tasks.Task> tasks = new List<System.Threading.Tasks.Task>();

            if (apiTargets.Count > 0 && _chatManager != null)
            {
                ChatRequestParams apiParams = new ChatRequestParams
                {
                    RequestPayload = requestPayload_,
                    Providers = apiTargets
                };

                tasks.Add(_chatManager.SendMessageToAllProvidersAsync(apiParams,
                    (ChatProviderType providerType_, ChatResponse response_) =>
                    {
                        ChatEditorProviderType editorType = ChatEditorProviderTypeUtility.FromApiProviderType(providerType_);
                        if (editorType != ChatEditorProviderType.NONE)
                        {
                            mainThread?.Post(_ => onProviderCompleted_?.Invoke(editorType, response_), null);
                        }
                    }, cancellationToken_));
            }

            if (cliTargets.Count > 0 && _cliManager != null)
            {
                ChatCliRequestParams cliParams = new ChatCliRequestParams
                {
                    RequestPayload = cliPayload_,
                    Providers = cliTargets
                };

                if (usePersistent_)
                {
                    foreach (ChatCliRequestProviderTarget cliTarget in cliTargets)
                    {
                        ChatCliRequestParams singleCliParams = new ChatCliRequestParams
                        {
                            RequestPayload = cliPayload_,
                            Providers = new List<ChatCliRequestProviderTarget> { cliTarget }
                        };

                        tasks.Add(System.Threading.Tasks.Task.Run(async () =>
                        {
                            try
                            {
                                ChatCliResponse cliResponse = await _cliManager.SendPersistentMessageWithProvidersAsync(singleCliParams, cancellationToken_);
                                ChatResponse response = MapCliResponse(cliResponse);
                                ChatEditorProviderType editorType = ChatEditorProviderTypeUtility.FromCliProviderType(cliTarget.ProviderType);
                                if (editorType != ChatEditorProviderType.NONE)
                                {
                                    mainThread?.Post(_ => onProviderCompleted_?.Invoke(editorType, response), null);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // Cancelled - do nothing
                            }
                            catch (Exception ex)
                            {
                                ChatResponse errorResponse = ChatResponse.FromError(ex.Message);
                                ChatEditorProviderType editorType = ChatEditorProviderTypeUtility.FromCliProviderType(cliTarget.ProviderType);
                                if (editorType != ChatEditorProviderType.NONE)
                                {
                                    mainThread?.Post(_ => onProviderCompleted_?.Invoke(editorType, errorResponse), null);
                                }
                            }
                        }));
                    }
                }
                else
                {
                    tasks.Add(_cliManager.SendMessageToAllProvidersAsync(cliParams,
                        (ChatCliProviderType providerType_, ChatCliResponse cliResponse_) =>
                        {
                            ChatResponse response = MapCliResponse(cliResponse_);
                            ChatEditorProviderType editorType = ChatEditorProviderTypeUtility.FromCliProviderType(providerType_);
                            if (editorType != ChatEditorProviderType.NONE)
                            {
                                mainThread?.Post(_ => onProviderCompleted_?.Invoke(editorType, response), null);
                            }
                        }, cancellationToken_));
                }
            }

            if (tasks.Count > 0)
                await System.Threading.Tasks.Task.WhenAll(tasks);
        }

        private List<ChatRequestProviderTarget> BuildApiTargets(List<ChatEditorProviderTarget> providerTargets_)
        {
            List<ChatRequestProviderTarget> targets = new List<ChatRequestProviderTarget>();
            if (providerTargets_ == null)
                return targets;

            foreach (ChatEditorProviderTarget target in providerTargets_)
            {
                if (!ChatEditorProviderTypeUtility.IsApiProvider(target.ProviderType))
                    continue;

                targets.Add(new ChatRequestProviderTarget
                {
                    ProviderType = ChatEditorProviderTypeUtility.ToApiProviderType(target.ProviderType),
                    Model = target.Model,
                    Priority = target.Priority
                });
            }

            return targets;
        }

        private List<ChatCliRequestProviderTarget> BuildCliTargets(List<ChatEditorProviderTarget> providerTargets_)
        {
            List<ChatCliRequestProviderTarget> targets = new List<ChatCliRequestProviderTarget>();
            if (providerTargets_ == null)
                return targets;

            foreach (ChatEditorProviderTarget target in providerTargets_)
            {
                if (!ChatEditorProviderTypeUtility.IsCliProvider(target.ProviderType))
                    continue;

                targets.Add(new ChatCliRequestProviderTarget
                {
                    ProviderType = ChatEditorProviderTypeUtility.ToCliProviderType(target.ProviderType),
                    Model = target.Model,
                    Priority = target.Priority
                });
            }

            return targets;
        }

        private ChatRequestParams BuildApiRequestParams(
            ChatRequestPayload requestPayload_,
            ChatProviderType providerType_,
            string model_,
            int priority_)
        {
            List<ChatRequestProviderTarget> targets = new List<ChatRequestProviderTarget>
            {
                new ChatRequestProviderTarget
                {
                    ProviderType = providerType_,
                    Model = model_,
                    Priority = priority_
                }
            };

            return new ChatRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = targets
            };
        }

        private ChatRequestParams BuildApiRequestParams(
            ChatRequestPayload requestPayload_,
            List<ChatRequestProviderTarget> targets_)
        {
            return new ChatRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = targets_
            };
        }

        private ChatCliRequestParams BuildCliRequestParams(
            ChatCliRequestPayload requestPayload_,
            ChatCliProviderType providerType_,
            string model_,
            int priority_)
        {
            List<ChatCliRequestProviderTarget> targets = new List<ChatCliRequestProviderTarget>
            {
                new ChatCliRequestProviderTarget
                {
                    ProviderType = providerType_,
                    Model = model_,
                    Priority = priority_
                }
            };

            return new ChatCliRequestParams
            {
                RequestPayload = requestPayload_,
                Providers = targets
            };
        }

        private ChatResponse MapCliResponse(ChatCliResponse cliResponse_)
        {
            if (cliResponse_ == null)
                return ChatResponse.FromError("CLI response was null.");

            if (!cliResponse_.IsSuccess)
            {
                ChatResponse errorResponse = ChatResponse.FromError(cliResponse_.ErrorMessage);
                errorResponse.Model = cliResponse_.Model;
                errorResponse.RawResponse = cliResponse_.RawResponse;
                return errorResponse;
            }

            ChatResponse response = new ChatResponse
            {
                Id = cliResponse_.Id,
                Model = cliResponse_.Model,
                Content = cliResponse_.Content,
                FinishReason = cliResponse_.FinishReason,
                IsSuccess = cliResponse_.IsSuccess,
                ErrorMessage = cliResponse_.ErrorMessage,
                RawResponse = cliResponse_.RawResponse
            };

            if (cliResponse_.Usage != null)
            {
                response.Usage = new ChatResponseUsageInfo
                {
                    PromptTokens = cliResponse_.Usage.PromptTokens,
                    CompletionTokens = cliResponse_.Usage.CompletionTokens,
                    TotalTokens = cliResponse_.Usage.TotalTokens
                };
            }

            return response;
        }

        private string BuildPriorityProviderLabel(ChatEditorProviderType providerType_, ChatResponse response_)
        {
            string returnString = LocalizationManager.Get(LocalizationKeys.PROVIDER_PRIORITY_PROVIDER);
            if (response_ == null)
            {
                return returnString;
            }

            ChatEditorProviderType resolvedProviderType = providerType_ != ChatEditorProviderType.NONE
                ? providerType_
                : response_.ProviderType != ChatProviderType.NONE
                    ? ChatEditorProviderTypeUtility.FromApiProviderType(response_.ProviderType)
                    : _chatProviderSection != null
                        ? _chatProviderSection.GetHighestPriorityProvider()
                        : ChatEditorProviderType.NONE;

            string providerLabel = resolvedProviderType != ChatEditorProviderType.NONE ? resolvedProviderType.ToString() : string.Empty;
            string modelLabel = GetChatModelDisplayName(resolvedProviderType, response_.Model);

            if (!string.IsNullOrEmpty(providerLabel) && !string.IsNullOrEmpty(modelLabel))
            {
                return $"{providerLabel} ({modelLabel})";
            }

            if (!string.IsNullOrEmpty(providerLabel))
            {
                return $"{providerLabel}";
            }

            if (!string.IsNullOrEmpty(modelLabel))
            {
                return $"{modelLabel}";
            }

            return returnString;
        }

        private string GetChatModelDisplayName(ChatEditorProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
            {
                return string.Empty;
            }

            if (providerType_ != ChatEditorProviderType.NONE)
            {
                ChatModelInfo modelInfo = _manager != null
                    ? _manager.GetModelInfo(providerType_, modelId_)
                    : null;
                if (modelInfo != null && !string.IsNullOrEmpty(modelInfo.DisplayName))
                {
                    return modelInfo.DisplayName;
                }
            }

            return modelId_;
        }

        private async System.Threading.Tasks.Task SendWithStreamingAsync(ChatRequestParams requestParams_)
        {
            // Get provider name and model for display
            string providerName = LocalizationManager.Get(LocalizationKeys.PROVIDER_PRIORITY_PROVIDER);
            string modelName = null;

            if (requestParams_?.Providers != null && requestParams_.Providers.Count > 0)
            {
                ChatRequestProviderTarget firstTarget = requestParams_.Providers[0];
                ChatEditorProviderType editorProviderType = ChatEditorProviderTypeUtility.FromApiProviderType(firstTarget.ProviderType);
                providerName = editorProviderType.ToString();
                modelName = firstTarget.Model;
            }

            VisualElement messageEntry = _conversationSection?.CreateStreamingMessageEntry(providerName, modelName);

            System.Text.StringBuilder contentBuilder = new System.Text.StringBuilder();
            int chunkCount = 0;

            try
            {
                await _chatManager.StreamMessageWithProvidersAsync(
                    requestParams_,
                    async (string chunk_) =>
                    {
                        chunkCount++;
                        contentBuilder.Append(chunk_);
                        _conversationSection?.UpdateStreamingContent(messageEntry, contentBuilder.ToString());

                        // CRITICAL: Yield control back to Unity main thread every 5 chunks to prevent freezing
                        if (chunkCount % 5 == 0)
                        {
                            await System.Threading.Tasks.Task.Yield();
                        }
                    },
                    _cancellationTokenSource.Token
                );

                SetSending(false);
                _conversationSection?.FinalizeStreamingEntry(messageEntry, true, LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS));
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_COMPLETED));
                _chatInputSection?.ClearAttachments();
            }
            catch (OperationCanceledException)
            {
                SetSending(false);
                _conversationSection?.FinalizeStreamingEntry(messageEntry, false, LocalizationManager.Get(LocalizationKeys.CHAT_CANCELLED));
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_CANCELLED));
            }
            catch (Exception ex)
            {
                SetSending(false);
                _conversationSection?.FinalizeStreamingEntry(messageEntry, false, LocalizationManager.Get(LocalizationKeys.COMMON_ERROR));
                _conversationSection?.UpdateStreamingContent(messageEntry, ex.Message);
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_FAILED, ex.Message));
            }
        }

        private ChatCliRequestPayload BuildCliPayload(ChatRequestPayload requestPayload_)
        {
            ChatCliRequestPayload cliPayload = new ChatCliRequestPayload();
            if (requestPayload_ == null)
                return cliPayload;

            if (!string.IsNullOrWhiteSpace(requestPayload_.SystemPrompt))
            {
                cliPayload.WithSystemPrompt(requestPayload_.SystemPrompt);
            }

            if (!string.IsNullOrEmpty(requestPayload_.Model))
            {
                cliPayload.Model = requestPayload_.Model;
            }

            if (requestPayload_.Temperature.HasValue)
            {
                cliPayload.WithTemperature(requestPayload_.Temperature.Value);
            }

            if (requestPayload_.MaxTokens.HasValue)
            {
                cliPayload.WithMaxTokens(requestPayload_.MaxTokens.Value);
            }

            if (requestPayload_.TopP.HasValue)
            {
                cliPayload.WithTopP(requestPayload_.TopP.Value);
            }

            if (requestPayload_.Stop != null && requestPayload_.Stop.Count > 0)
            {
                cliPayload.WithStop(new List<string>(requestPayload_.Stop));
            }

            if (requestPayload_.AdditionalBodyParameters != null)
            {
                cliPayload.WithAdditionalBodyParameters(new Dictionary<string, object>(requestPayload_.AdditionalBodyParameters));
            }

            if (requestPayload_.Messages != null)
            {
                foreach (ChatRequestMessage message in requestPayload_.Messages)
                {
                    if (message == null)
                        continue;

                    ChatCliRequestMessageRoleType role = MapCliRole(message.RequestMessageRoleType);
                    string content = ResolveMessageContent(message);
                    cliPayload.AddMessage(new ChatCliRequestMessage(role, content));
                }
            }

            return cliPayload;
        }

        private ChatCliRequestMessageRoleType MapCliRole(ChatRequestMessageRoleType role_)
        {
            return role_ switch
            {
                ChatRequestMessageRoleType.SYSTEM => ChatCliRequestMessageRoleType.SYSTEM,
                ChatRequestMessageRoleType.ASSISTANT => ChatCliRequestMessageRoleType.ASSISTANT,
                _ => ChatCliRequestMessageRoleType.USER
            };
        }

        private string ResolveMessageContent(ChatRequestMessage message_)
        {
            if (message_ == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(message_.Content))
                return message_.Content;

            if (message_.MultiContent == null || message_.MultiContent.Count == 0)
                return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            foreach (ChatRequestMessageContent content in message_.MultiContent)
            {
                if (content == null)
                    continue;

                if (content.Type == "text" && !string.IsNullOrEmpty(content.Text))
                {
                    builder.AppendLine(content.Text);
                }
                else if (content.Type == "image")
                {
                    builder.AppendLine("[image]");
                }
                else if (content.Type == "image_url")
                {
                    builder.AppendLine("[image_url]");
                }
                else if (content.Type == "document")
                {
                    string fileName = content.Document != null ? content.Document.FileName : string.Empty;
                    builder.AppendLine(string.IsNullOrEmpty(fileName) ? "[document]" : $"[document:{fileName}]");
                }
                else if (content.Type == "text_file")
                {
                    builder.AppendLine("[text_file]");
                }
            }

            return builder.ToString().Trim();
        }

        private ChatRequestPayload BuildChatPayload(ChatInputData inputData_)
        {
            ChatRequestPayload requestPayload = new ChatRequestPayload();

            if (inputData_.AttachedFiles != null && inputData_.AttachedFiles.Count > 0)
            {
                ChatRequestMessage userRequestMessageBase = new ChatRequestMessage();
                userRequestMessageBase.RequestMessageRoleType = ChatRequestMessageRoleType.USER;
                userRequestMessageBase.MultiContent = new List<ChatRequestMessageContent>();

                userRequestMessageBase.MultiContent.Add(ChatRequestMessageContent.CreateText(inputData_.UserPrompt));

                foreach (AttachedFileData attachedFile in inputData_.AttachedFiles)
                {
                    switch (attachedFile.FileType)
                    {
                        case AttachedFileType.IMAGE:
                            userRequestMessageBase.MultiContent.Add(ChatRequestMessageContent.CreateImage(
                                                                        attachedFile.Base64Data,
                                                                        attachedFile.MediaType
                                                                    ));
                            break;

                        case AttachedFileType.PDF:
                            userRequestMessageBase.MultiContent.Add(ChatRequestMessageContent.CreateDocument(
                                                                        attachedFile.Base64Data,
                                                                        attachedFile.MediaType,
                                                                        attachedFile.FileName
                                                                    ));
                            break;

                        case AttachedFileType.TEXT:
                            userRequestMessageBase.MultiContent.Add(ChatRequestMessageContent.CreateTextFile(
                                                                        attachedFile.TextContent,
                                                                        attachedFile.MediaType,
                                                                        attachedFile.FileName
                                                                    ));
                            break;
                    }
                }

                requestPayload.AddMessage(userRequestMessageBase);
            }
            else
            {
                requestPayload.AddUserMessage(inputData_.UserPrompt);
            }

            if (!string.IsNullOrWhiteSpace(inputData_.SystemPrompt))
            {
                requestPayload.WithSystemPrompt(inputData_.SystemPrompt);
            }

            return requestPayload;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _chatManager?.Dispose();
            _cliManager?.Dispose();

            LocalizationManager.OnLanguageChanged -= UpdateSectionHeaders;

            _chatInputSection?.UnregisterCancelCallback(OnCancelClicked);

            if (_chatInputSection != null)
                _chatInputSection.OnResetSessionRequested -= HandleResetSessionRequested;

            _chatProviderSection?.Dispose();
            _conversationSection?.Dispose();
            _chatInputSection?.Dispose();
        }
    }
}
