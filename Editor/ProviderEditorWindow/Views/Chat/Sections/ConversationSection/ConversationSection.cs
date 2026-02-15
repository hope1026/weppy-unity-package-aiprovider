using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Chat.Editor
{
    public class ConversationSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "Chat/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "ConversationSection/ConversationSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "ConversationSection/ConversationSection.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnClearRequested;

        private VisualElement _messagesContainer;
        private DropdownField _historyDropdown;
        private Button _historyLoadButton;
        private Label _historyLabel;
        private List<HistoryEntryInfo> _historyEntries = new List<HistoryEntryInfo>();
        private List<ChatHistoryMessage> _messageHistory = new List<ChatHistoryMessage>();
        private Dictionary<VisualElement, int> _streamingMessageIndexByEntry = new Dictionary<VisualElement, int>();
        private bool _suppressHistorySave;

        public ConversationSection()
        {
            LoadLayout();
            LoadStyles();
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

        private void SetStatus(string message_)
        {
            OnStatusChanged?.Invoke(message_);
        }

        private void SetupUI()
        {
            _messagesContainer = this.Q<VisualElement>("chat-messages-container");
            _historyDropdown = this.Q<DropdownField>("history-dropdown");
            _historyLoadButton = this.Q<Button>("history-load-button");
            _historyLabel = this.Q<Label>("history-label");

            Button clearButton = this.Q<Button>("clear-chat-button");
            if (clearButton != null)
            {
                clearButton.clicked += OnClearClicked;
            }

            if (_historyLoadButton != null)
            {
                _historyLoadButton.clicked += OnHistoryLoadClicked;
            }

            UpdateHistoryUI();
            RefreshHistoryList();
        }

        private void OnClearClicked()
        {
            SaveHistoryIfNeeded();
            ClearMessages();
            OnClearRequested?.Invoke();
        }

        public void ClearMessages()
        {
            _messagesContainer?.Clear();
            _messageHistory.Clear();
            _streamingMessageIndexByEntry.Clear();
        }

        public void AddUserMessage(string message_, int attachmentCount_)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            AddUserMessageInternal(message_, attachmentCount_, timestamp);
            ChatHistoryMessage messageData = new ChatHistoryMessage
            {
                MessageType = ChatHistoryMessageType.USER,
                Content = message_,
                Timestamp = timestamp,
                AttachmentCount = attachmentCount_
            };
            _messageHistory.Add(messageData);
        }

        public void AddResponseMessage(string providerName_, ChatResponse response_)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            AddResponseMessageInternal(providerName_, response_, timestamp);

            ChatHistoryMessage messageData = new ChatHistoryMessage
            {
                MessageType = ChatHistoryMessageType.RESPONSE,
                ProviderName = providerName_,
                Timestamp = timestamp,
                IsSuccess = response_.IsSuccess,
                Content = response_.IsSuccess ? response_.Content : null,
                ErrorMessage = response_.IsSuccess ? null : response_.ErrorMessage
            };

            if (response_.Usage != null)
            {
                messageData.HasUsage = true;
                messageData.PromptTokens = response_.Usage.PromptTokens;
                messageData.CompletionTokens = response_.Usage.CompletionTokens;
                messageData.TotalTokens = response_.Usage.TotalTokens;
            }

            _messageHistory.Add(messageData);
        }

        public VisualElement CreateStreamingMessageEntry(string providerName_, string modelName_ = null)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            VisualElement messageEntry = CreateStreamingMessageEntryInternal(providerName_, modelName_, timestamp);

            ChatHistoryMessage messageData = new ChatHistoryMessage
            {
                MessageType = ChatHistoryMessageType.RESPONSE,
                ProviderName = providerName_,
                Timestamp = timestamp,
                IsStreaming = true
            };

            _messageHistory.Add(messageData);
            _streamingMessageIndexByEntry[messageEntry] = _messageHistory.Count - 1;

            return messageEntry;
        }

        private int _updateCount = 0;

        public void UpdateStreamingContent(VisualElement entry_, string content_)
        {
            if (entry_ == null)
                return;

            TextField contentField = entry_.Q<TextField>(className: "message-content");
            if (contentField != null)
            {
                _updateCount++;
                contentField.value = content_;
            }

            if (_streamingMessageIndexByEntry.TryGetValue(entry_, out int messageIndex))
            {
                if (messageIndex >= 0 && messageIndex < _messageHistory.Count)
                {
                    _messageHistory[messageIndex].Content = content_;
                }
            }
            
            if (_updateCount % 10 == 0)
            {
                ScrollToBottom();
            }
        }

        public void FinalizeStreamingEntry(VisualElement entry_, bool success_, string statusText_)
        {
            if (entry_ == null)
                return;
            
            _updateCount = 0; // Reset counter for next stream

            Label statusLabel = entry_.Q<Label>(className: "message-status");
            if (statusLabel != null)
            {
                statusLabel.text = statusText_;
                statusLabel.RemoveFromClassList("streaming");
                statusLabel.AddToClassList(success_ ? "success" : "error");
            }

            if (_streamingMessageIndexByEntry.TryGetValue(entry_, out int messageIndex))
            {
                if (messageIndex >= 0 && messageIndex < _messageHistory.Count)
                {
                    ChatHistoryMessage messageData = _messageHistory[messageIndex];
                    messageData.IsStreaming = false;
                    messageData.IsSuccess = success_;
                    if (!success_)
                    {
                        messageData.ErrorMessage = statusText_;
                    }
                }
            }
        }

        public void SaveHistoryIfNeeded()
        {
            if (_suppressHistorySave)
                return;

            if (_messageHistory.Count == 0)
                return;

            ChatHistorySnapshot snapshot = BuildSnapshot();
            if (snapshot == null)
                return;

            if (EditorHistoryStorage.SaveChatHistory(snapshot))
            {
                RefreshHistoryList();
            }
        }

        public void LoadHistory(string entryId_)
        {
            if (string.IsNullOrEmpty(entryId_))
                return;

            ChatHistorySnapshot snapshot = EditorHistoryStorage.LoadChatHistory(entryId_);
            if (snapshot == null || snapshot.Messages == null)
                return;

            _suppressHistorySave = true;
            ClearMessages();

            foreach (ChatHistoryMessage message in snapshot.Messages)
            {
                if (message.MessageType == ChatHistoryMessageType.USER)
                {
                    AddUserMessageInternal(message.Content, message.AttachmentCount, message.Timestamp);
                    _messageHistory.Add(message);
                }
                else if (message.MessageType == ChatHistoryMessageType.RESPONSE)
                {
                    ChatResponse response = new ChatResponse();
                    response.IsSuccess = message.IsSuccess;
                    response.Content = message.Content;
                    response.ErrorMessage = message.ErrorMessage;
                    if (message.HasUsage)
                    {
                        response.Usage = new ChatResponseUsageInfo(
                            message.PromptTokens,
                            message.CompletionTokens);
                        response.Usage.TotalTokens = message.TotalTokens;
                    }

                    AddResponseMessageInternal(message.ProviderName, response, message.Timestamp);
                    _messageHistory.Add(message);
                }
            }

            _suppressHistorySave = false;
        }

        public void ScrollToBottom()
        {
            ScrollView scrollView = this.Q<ScrollView>("chat-messages-scroll");
            if (scrollView != null)
            {
                scrollView.schedule.Execute(() =>
                {
                    scrollView.scrollOffset = new Vector2(0, scrollView.contentContainer.layout.height);
                }).ExecuteLater(10);
            }
        }

        public void Dispose()
        {
            Button clearButton = this.Q<Button>("clear-chat-button");
            if (clearButton != null)
            {
                clearButton.clicked -= OnClearClicked;
            }

            if (_historyLoadButton != null)
            {
                _historyLoadButton.clicked -= OnHistoryLoadClicked;
            }
        }

        private void AddUserMessageInternal(string message_, int attachmentCount_, string timestamp_)
        {
            VisualElement messageEntry = new VisualElement();
            messageEntry.AddToClassList("message-entry");
            messageEntry.AddToClassList("message-sent");

            VisualElement header = new VisualElement();
            header.AddToClassList("message-header");

            Label roleLabel = new Label("You");
            roleLabel.AddToClassList("message-role");

            Label timestampLabel = new Label(timestamp_);
            timestampLabel.AddToClassList("message-timestamp");

            header.Add(roleLabel);
            header.Add(timestampLabel);

            TextField contentField = new TextField();
            contentField.value = message_;
            contentField.isReadOnly = true;
            contentField.multiline = true;
            contentField.AddToClassList("message-content");
            contentField.AddToClassList("selectable-content");

            messageEntry.Add(header);
            messageEntry.Add(contentField);

            if (attachmentCount_ > 0)
            {
                Label attachmentLabel = new Label($"[{attachmentCount_} attachment(s)]");
                attachmentLabel.AddToClassList("message-attachment-indicator");
                messageEntry.Add(attachmentLabel);
            }

            _messagesContainer?.Add(messageEntry);
            ScrollToBottom();
        }

        private void AddResponseMessageInternal(string providerName_, ChatResponse response_, string timestamp_)
        {
            VisualElement messageEntry = new VisualElement();
            messageEntry.AddToClassList("message-entry");
            messageEntry.AddToClassList("message-received");

            VisualElement header = new VisualElement();
            header.AddToClassList("message-header");

            Label providerLabel = new Label(providerName_);
            providerLabel.AddToClassList("message-provider-name");

            Label timestampLabel = new Label(timestamp_);
            timestampLabel.AddToClassList("message-timestamp");

            string statusText = response_.IsSuccess
                ? LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS)
                : LocalizationManager.Get(LocalizationKeys.COMMON_ERROR);
            Label statusLabel = new Label(statusText);
            statusLabel.AddToClassList("message-status");
            statusLabel.AddToClassList(response_.IsSuccess ? "success" : "error");

            header.Add(providerLabel);
            header.Add(timestampLabel);
            header.Add(statusLabel);

            TextField contentField = new TextField();
            contentField.value = response_.IsSuccess ? response_.Content : response_.ErrorMessage;
            contentField.isReadOnly = true;
            contentField.multiline = true;
            contentField.AddToClassList("message-content");
            contentField.AddToClassList("selectable-content");

            messageEntry.Add(header);
            messageEntry.Add(contentField);

            if (response_.Usage != null)
            {
                Label usageLabel = new Label(LocalizationManager.Get(
                    LocalizationKeys.CHAT_TOKENS,
                    response_.Usage.PromptTokens,
                    response_.Usage.CompletionTokens,
                    response_.Usage.TotalTokens));
                usageLabel.AddToClassList("token-usage-label");
                messageEntry.Add(usageLabel);
            }

            _messagesContainer?.Add(messageEntry);
            ScrollToBottom();
        }

        private VisualElement CreateStreamingMessageEntryInternal(string providerName_, string modelName_, string timestamp_)
        {
            VisualElement messageEntry = new VisualElement();
            messageEntry.AddToClassList("message-entry");
            messageEntry.AddToClassList("message-received");

            VisualElement header = new VisualElement();
            header.AddToClassList("message-header");

            // Provider and model name
            string displayName = string.IsNullOrEmpty(modelName_)
                ? providerName_
                : $"{providerName_} ({modelName_})";
            Label providerLabel = new Label(displayName);
            providerLabel.AddToClassList("message-provider-name");

            Label timestampLabel = new Label(timestamp_);
            timestampLabel.AddToClassList("message-timestamp");

            Label statusLabel = new Label(LocalizationManager.Get(LocalizationKeys.CHAT_STREAMING));
            statusLabel.AddToClassList("message-status");
            statusLabel.AddToClassList("streaming");

            header.Add(providerLabel);
            header.Add(timestampLabel);
            header.Add(statusLabel);

            TextField contentField = new TextField();
            contentField.value = "";
            contentField.isReadOnly = true;
            contentField.multiline = true;
            contentField.AddToClassList("message-content");
            contentField.AddToClassList("selectable-content");

            messageEntry.Add(header);
            messageEntry.Add(contentField);

            _messagesContainer?.Add(messageEntry);
            ScrollToBottom();

            return messageEntry;
        }

        private void OnHistoryLoadClicked()
        {
            if (_historyEntries == null || _historyEntries.Count == 0)
                return;

            if (_historyDropdown == null)
                return;

            int index = _historyDropdown.index;
            if (index < 0 || index >= _historyEntries.Count)
                return;

            LoadHistory(_historyEntries[index].Id);
        }

        private void UpdateHistoryUI()
        {
            if (_historyLabel != null)
            {
                _historyLabel.text = LocalizationManager.Get(LocalizationKeys.HISTORY_LABEL);
            }

            if (_historyLoadButton != null)
            {
                _historyLoadButton.text = LocalizationManager.Get(LocalizationKeys.HISTORY_LOAD);
            }
        }

        private void RefreshHistoryList()
        {
            _historyEntries = EditorHistoryStorage.ListEntries(HistoryFeatureType.CHAT);
            List<string> displayNames = new List<string>();
            foreach (HistoryEntryInfo entry in _historyEntries)
            {
                displayNames.Add(string.IsNullOrEmpty(entry.DisplayName) ? entry.CreatedAt : entry.DisplayName);
            }

            if (_historyDropdown != null)
            {
                _historyDropdown.choices = displayNames;
                if (displayNames.Count == 0)
                {
                    _historyDropdown.choices = new List<string> { LocalizationManager.Get(LocalizationKeys.HISTORY_EMPTY) };
                    _historyDropdown.index = 0;
                    _historyDropdown.SetEnabled(false);
                }
                else
                {
                    _historyDropdown.index = 0;
                    _historyDropdown.SetEnabled(true);
                }
            }

            if (_historyLoadButton != null)
            {
                _historyLoadButton.SetEnabled(displayNames.Count > 0);
            }
        }

        private ChatHistorySnapshot BuildSnapshot()
        {
            if (_messageHistory.Count == 0)
                return null;

            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ChatHistorySnapshot snapshot = new ChatHistorySnapshot
            {
                Id = EditorHistoryStorage.CreateEntryId(),
                CreatedAt = createdAt,
                DisplayName = createdAt,
                ItemCount = _messageHistory.Count,
                Messages = new List<ChatHistoryMessage>(_messageHistory)
            };

            return snapshot;
        }
    }
}
