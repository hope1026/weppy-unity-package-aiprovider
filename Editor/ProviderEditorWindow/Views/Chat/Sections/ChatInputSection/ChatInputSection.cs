using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ChatInputSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "Chat/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "ChatInputSection/ChatInputSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "ChatInputSection/ChatInputSection.uss";

        public event Action<string> OnStatusChanged;
        public event Action<ChatInputData> OnSendRequested;
        public event Action OnNavigateToSettingsRequested;
        public event Action<bool> OnCliPersistentProcessChanged;
        public event Action OnResetSessionRequested;

        private EditorDataStorage _storage;

        private TextField _systemPromptField;
        private TextField _chatPromptField;
        private Button _sendButton;
        private DropdownField _sendModeDropdown;
        private Toggle _streamToggle;
        private Label _streamLabel;
        private Toggle _persistentToggle;
        private Label _persistentLabel;
        private Button _resetSessionButton;

        private VisualElement _loadingContainer;
        private Label _loadingLabel;
        private Button _cancelButton;

        private VisualElement _imageAttachmentContainer;
        private VisualElement _imagePreviewContainer;
        private VisualElement _textureDropArea;
        private Button _addAttachmentButton;
        private VisualElement _attachmentPopup;
        private Button _clearImagesButton;
        private Label _attachmentHint;
        private Label _attachmentWarningLabel;
        private Label _dropAreaHint;

        private List<AttachedFileData> _attachedFiles = new List<AttachedFileData>();
        private IMGUIContainer _imguiContainer;

        private static int _objectPickerControlId = 0;
        private bool _waitingForObjectPicker = false;

        private bool _isSending;
        private bool _isEnabled = true;
        private bool _isCliPersistentVisible;

        private Func<bool> _canSendCallback;

        public ChatInputSection()
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

        public void Initialize(Func<bool> canSendCallback_, EditorDataStorage storage_)
        {
            _canSendCallback = canSendCallback_;
            _storage = storage_;
            UpdateLocalization();
        }

        private void SetupUI()
        {
            _systemPromptField = this.Q<TextField>("system-prompt-field");
            _chatPromptField = this.Q<TextField>("chat-prompt-field");

            if (_systemPromptField != null)
            {
                _systemPromptField.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_SYSTEM_PROMPT_TOOLTIP);
            }
            if (_chatPromptField != null)
            {
                _chatPromptField.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_USER_PROMPT_TOOLTIP);
                _chatPromptField.RegisterCallback<KeyDownEvent>(HandlePromptKeyDown, TrickleDown.TrickleDown);
            }

            this.RegisterCallback<KeyDownEvent>(HandleGlobalKeyDown, TrickleDown.TrickleDown);
            this.focusable = true;

            _sendButton = this.Q<Button>("send-chat-button");
            if (_sendButton != null)
            {
                _sendButton.clicked += OnSendClicked;
            }

            _sendModeDropdown = this.Q<DropdownField>("send-mode-dropdown");
            if (_sendModeDropdown != null)
            {
                _sendModeDropdown.choices = new List<string>
                {
                    LocalizationManager.Get(LocalizationKeys.CHAT_SEND_MODE_PRIORITY),
                    LocalizationManager.Get(LocalizationKeys.CHAT_SEND_MODE_ALL)
                };
                _sendModeDropdown.index = 0;
                _sendModeDropdown.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_SEND_MODE_TOOLTIP);
            }

            _streamToggle = this.Q<Toggle>("stream-toggle");
            _streamLabel = this.Q<Label>("stream-label");
            if (_streamToggle != null)
            {
                _streamToggle.value = false;
                _streamToggle.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_STREAM_TOOLTIP);
            }
            if (_streamLabel != null)
            {
                _streamLabel.text = LocalizationManager.Get(LocalizationKeys.CHAT_STREAM_LABEL);
            }

            _persistentToggle = this.Q<Toggle>("persistent-toggle");
            _persistentLabel = this.Q<Label>("persistent-label");
            _resetSessionButton = this.Q<Button>("reset-session-button");

            if (_persistentToggle != null)
            {
                _persistentToggle.value = false;
                _persistentToggle.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_CLI_PERSISTENT_TOOLTIP);
                _persistentToggle.RegisterValueChangedCallback(HandlePersistentToggleChanged);
            }
            if (_persistentLabel != null)
            {
                _persistentLabel.text = LocalizationManager.Get(LocalizationKeys.CHAT_CLI_PERSISTENT_LABEL);
            }
            if (_resetSessionButton != null)
            {
                _resetSessionButton.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_CLI_RESET_SESSION_TOOLTIP);
                _resetSessionButton.clicked += OnResetSessionClicked;
            }

            SetupAttachmentUI();
            CreateLoadingIndicator();
            SetupIMGUIContainer();
            SetupDragAndDrop();
            SetupTextureDrop();
            RefreshFilePreviews();
        }

        private void SetupAttachmentUI()
        {
            _imageAttachmentContainer = this.Q<VisualElement>("image-attachment-container");
            _imagePreviewContainer = this.Q<VisualElement>("image-preview-container");
            _textureDropArea = this.Q<VisualElement>("texture-drop-area");
            _dropAreaHint = _textureDropArea?.Q<Label>(className: "drop-area-hint");
            _addAttachmentButton = this.Q<Button>("add-attachment-button");
            _clearImagesButton = this.Q<Button>("clear-images-button");
            _attachmentHint = this.Q<Label>("attachment-hint");

            _attachmentWarningLabel = new Label();
            _attachmentWarningLabel.AddToClassList("attachment-warning-label");
            _attachmentWarningLabel.style.display = DisplayStyle.None;
            if (_imageAttachmentContainer != null)
            {
                _imageAttachmentContainer.Add(_attachmentWarningLabel);
            }

            if (_addAttachmentButton != null)
            {
                _addAttachmentButton.clicked += ShowAttachmentPopup;
            }

            if (_clearImagesButton != null)
            {
                _clearImagesButton.clicked += HandleClearImagesClicked;
            }
        }

        private void CreateLoadingIndicator()
        {
            _loadingContainer = new VisualElement();
            _loadingContainer.AddToClassList("loading-container");
            _loadingContainer.style.display = DisplayStyle.None;

            _loadingLabel = new Label(LocalizationManager.Get(LocalizationKeys.CHAT_SENDING));
            _loadingLabel.AddToClassList("loading-label");

            _cancelButton = new Button();
            _cancelButton.text = LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL);
            _cancelButton.AddToClassList("cancel-button");

            _loadingContainer.Add(_loadingLabel);
            _loadingContainer.Add(_cancelButton);

            VisualElement buttonsContainer = _sendButton?.parent;
            if (buttonsContainer != null)
            {
                buttonsContainer.Add(_loadingContainer);
            }
            else
            {
                Add(_loadingContainer);
            }
        }

        private void SetupIMGUIContainer()
        {
            _imguiContainer = new IMGUIContainer(OnGUIHandler);
            _imguiContainer.style.position = Position.Absolute;
            _imguiContainer.style.width = 0;
            _imguiContainer.style.height = 0;
            Add(_imguiContainer);
        }

        private void SetupDragAndDrop()
        {
            ImageDragDropHelper.SetupFileDragAndDrop(
                _imageAttachmentContainer,
                LoadImageFromPath
            );

            if (_chatPromptField != null)
            {
                ImageDragDropHelper.SetupFileDragAndDrop(
                    _chatPromptField,
                    LoadImageFromPath
                );
            }
        }

        private void SetupTextureDrop()
        {
            ImageDragDropHelper.SetupTextureDragAndDrop(
                _textureDropArea,
                AddTextureFromProject
            );
        }

        public void UpdateLocalization()
        {
            if (_sendModeDropdown != null)
            {
                int currentIndex = _sendModeDropdown.index;
                _sendModeDropdown.choices = new List<string>
                {
                    LocalizationManager.Get(LocalizationKeys.CHAT_SEND_MODE_PRIORITY),
                    LocalizationManager.Get(LocalizationKeys.CHAT_SEND_MODE_ALL)
                };
                _sendModeDropdown.index = currentIndex;
                _sendModeDropdown.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_SEND_MODE_TOOLTIP);
            }

            if (_streamLabel != null)
            {
                _streamLabel.text = LocalizationManager.Get(LocalizationKeys.CHAT_STREAM_LABEL);
            }
            if (_streamToggle != null)
            {
                _streamToggle.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_STREAM_TOOLTIP);
            }

            if (_persistentLabel != null)
            {
                _persistentLabel.text = LocalizationManager.Get(LocalizationKeys.CHAT_CLI_PERSISTENT_LABEL);
            }
            if (_persistentToggle != null)
            {
                _persistentToggle.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_CLI_PERSISTENT_TOOLTIP);
            }
            if (_resetSessionButton != null)
            {
                _resetSessionButton.text = LocalizationManager.Get(LocalizationKeys.CHAT_CLI_RESET_SESSION);
                _resetSessionButton.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_CLI_RESET_SESSION_TOOLTIP);
            }

            if (_systemPromptField != null)
            {
                _systemPromptField.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_SYSTEM_PROMPT_TOOLTIP);
            }
            if (_chatPromptField != null)
            {
                _chatPromptField.tooltip = LocalizationManager.Get(LocalizationKeys.CHAT_USER_PROMPT_TOOLTIP);
            }

            if (_dropAreaHint != null)
            {
                _dropAreaHint.text = LocalizationManager.Get(LocalizationKeys.CHAT_DROP_AREA_HINT);
            }
            if (_attachmentHint != null)
            {
                _attachmentHint.text = LocalizationManager.Get(LocalizationKeys.CHAT_ATTACHMENT_HINT);
            }
        }

        public void SetInputEnabled(bool enabled_)
        {
            _isEnabled = enabled_;
            UpdateUIState();
        }

        public void SetSending(bool isSending_, string message_ = "Sending...")
        {
            _isSending = isSending_;

            if (_loadingContainer != null)
                _loadingContainer.style.display = isSending_ ? DisplayStyle.Flex : DisplayStyle.None;

            if (_loadingLabel != null)
                _loadingLabel.text = message_;

            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool canInteract = _isEnabled && !_isSending;

            if (_sendButton != null)
                _sendButton.SetEnabled(canInteract);
            if (_sendModeDropdown != null)
                _sendModeDropdown.SetEnabled(canInteract);
            if (_streamToggle != null)
                _streamToggle.SetEnabled(canInteract);
            if (_addAttachmentButton != null)
                _addAttachmentButton.SetEnabled(canInteract);
            if (_persistentToggle != null)
                _persistentToggle.SetEnabled(canInteract);
            if (_resetSessionButton != null)
                _resetSessionButton.SetEnabled(canInteract);
        }

        public void RegisterCancelCallback(Action onCancel_)
        {
            if (_cancelButton != null)
            {
                _cancelButton.clicked += onCancel_;
            }
        }

        public void UnregisterCancelCallback(Action onCancel_)
        {
            if (_cancelButton != null)
            {
                _cancelButton.clicked -= onCancel_;
            }
        }

        private void OnSendClicked()
        {
            if (_canSendCallback != null && !_canSendCallback())
            {
                ShowFeatureDisabledWarning();
                return;
            }

            string userPrompt = _chatPromptField?.value;
            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_PLEASE_ENTER_MESSAGE));
                return;
            }

            ChatInputData inputData = new ChatInputData
            {
                UserPrompt = userPrompt,
                SystemPrompt = _systemPromptField?.value,
                AttachedFiles = new List<AttachedFileData>(_attachedFiles),
                SendToAll = _sendModeDropdown != null && _sendModeDropdown.index == 1,
                UseStreaming = _streamToggle != null && _streamToggle.value,
                UsePersistent = _persistentToggle != null && _persistentToggle.value
            };

            OnSendRequested?.Invoke(inputData);
        }

        private void HandleCliPersistentProcessChanged(ChangeEvent<bool> evt_)
        {
            if (!_isCliPersistentVisible)
                return;

            OnCliPersistentProcessChanged?.Invoke(evt_.newValue);
        }

        private void HandlePersistentToggleChanged(ChangeEvent<bool> evt_)
        {
            if (_resetSessionButton != null)
            {
                _resetSessionButton.style.display = evt_.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            }

            OnCliPersistentProcessChanged?.Invoke(evt_.newValue);
        }

        private void OnResetSessionClicked()
        {
            OnResetSessionRequested?.Invoke();
        }

        public void SetPersistentControlsVisible(bool visible_)
        {
            _isCliPersistentVisible = visible_;
            DisplayStyle display = visible_ ? DisplayStyle.Flex : DisplayStyle.None;

            if (_persistentToggle != null)
                _persistentToggle.style.display = display;
            if (_persistentLabel != null)
                _persistentLabel.style.display = display;

            if (!visible_)
            {
                if (_resetSessionButton != null)
                    _resetSessionButton.style.display = DisplayStyle.None;
            }
            else if (_persistentToggle != null && _persistentToggle.value)
            {
                if (_resetSessionButton != null)
                    _resetSessionButton.style.display = DisplayStyle.Flex;
            }
        }

        private void ShowFeatureDisabledWarning()
        {
            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_NO_ENABLED_PROVIDER));

            bool goToSettings = EditorUtility.DisplayDialog(
                LocalizationManager.Get(LocalizationKeys.CHAT_FEATURE_DISABLED_TITLE),
                LocalizationManager.Get(LocalizationKeys.CHAT_NO_ENABLED_PROVIDER),
                LocalizationManager.Get(LocalizationKeys.CHAT_GO_TO_SETTINGS),
                LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
            );

            if (goToSettings)
            {
                OnNavigateToSettingsRequested?.Invoke();
            }
        }

        public ChatInputData GetInputData()
        {
            return new ChatInputData
            {
                UserPrompt = _chatPromptField?.value,
                SystemPrompt = _systemPromptField?.value,
                AttachedFiles = new List<AttachedFileData>(_attachedFiles),
                SendToAll = _sendModeDropdown != null && _sendModeDropdown.index == 1,
                UseStreaming = _streamToggle != null && _streamToggle.value,
                UsePersistent = _persistentToggle != null && _persistentToggle.value
            };
        }

        public List<AttachedFileData> GetAttachedFiles()
        {
            return new List<AttachedFileData>(_attachedFiles);
        }

        public int GetAttachmentCount()
        {
            return _attachedFiles.Count;
        }

        public void ClearAttachments()
        {
            foreach (AttachedFileData attachedFile in _attachedFiles)
            {
                attachedFile.DisposeThumbnail();
            }
            _attachedFiles.Clear();
            RefreshFilePreviews();
        }

        private void ShowAttachmentPopup()
        {
            if (_attachmentPopup != null)
            {
                CloseAttachmentPopup();
                return;
            }

            _attachmentPopup = CreateAttachmentPopup();
            this.Add(_attachmentPopup);

            _attachmentPopup.style.position = Position.Absolute;

            if (_addAttachmentButton != null)
            {
                Rect buttonRect = _addAttachmentButton.worldBound;
                Rect thisRect = this.worldBound;

                float distanceFromBottom = thisRect.yMax - buttonRect.y + 4;
                float relativeRight = thisRect.xMax - buttonRect.xMax;

                _attachmentPopup.style.bottom = distanceFromBottom;
                _attachmentPopup.style.right = relativeRight;
                _attachmentPopup.style.left = StyleKeyword.Auto;
                _attachmentPopup.style.top = StyleKeyword.Auto;
            }
        }

        private void CloseAttachmentPopup()
        {
            if (_attachmentPopup != null)
            {
                _attachmentPopup.RemoveFromHierarchy();
                _attachmentPopup = null;
            }
        }

        private VisualElement CreateAttachmentPopup()
        {
            VisualElement popup = new VisualElement();
            popup.AddToClassList("attachment-popup");

            Button addFileButton = new Button(() =>
            {
                CloseAttachmentPopup();
                HandleAddFileOrImageClicked();
            });
            addFileButton.text = LocalizationManager.Get(LocalizationKeys.CHAT_ADD_FILE);
            addFileButton.AddToClassList("attachment-popup-item");
            popup.Add(addFileButton);

            Button addTextureButton = new Button(() =>
            {
                CloseAttachmentPopup();
                HandleAddProjectTextureClicked();
            });
            addTextureButton.text = LocalizationManager.Get(LocalizationKeys.CHAT_ADD_TEXTURE);
            addTextureButton.AddToClassList("attachment-popup-item");
            popup.Add(addTextureButton);

            popup.RegisterCallback<FocusOutEvent>(evt =>
            {
                popup.schedule.Execute(CloseAttachmentPopup).ExecuteLater(100);
            });

            return popup;
        }

        private void HandleAddFileOrImageClicked()
        {
            string allExtensions = "png,jpg,jpeg,gif,webp," + FileAttachmentHelper.FILE_DIALOG_EXTENSIONS;
            string lastPath = _storage != null
                ? _storage.GetString(EditorDataStorageKeys.KEY_LAST_CHAT_ATTACHMENT_PATH, "")
                : "";
            string initialDirectory = string.IsNullOrEmpty(lastPath) ? "" : lastPath;

            string path = EditorUtility.OpenFilePanel(
                LocalizationManager.Get(LocalizationKeys.CHAT_ADD_FILE),
                initialDirectory,
                allExtensions
            );

            if (!string.IsNullOrEmpty(path))
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    _storage?.SetString(EditorDataStorageKeys.KEY_LAST_CHAT_ATTACHMENT_PATH, directory);
                }
                LoadImageFromPath(path);
            }
        }

        private void HandleAddProjectTextureClicked()
        {
            _objectPickerControlId = GUIUtility.GetControlID(FocusType.Passive) + 100;
            _waitingForObjectPicker = true;
            EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", _objectPickerControlId);
        }

        private void HandleClearImagesClicked()
        {
            if (_attachedFiles.Count > 0)
            {
                ClearAttachments();
                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_IMAGES_CLEARED));
            }
        }

        private void HandlePromptKeyDown(KeyDownEvent evt_)
        {
            bool isCtrlV = evt_.ctrlKey && evt_.keyCode == KeyCode.V;
            bool isCmdV = evt_.commandKey && evt_.keyCode == KeyCode.V;

            if (isCtrlV || isCmdV)
            {
                if (TryLoadImageFromClipboard())
                {
                    evt_.StopPropagation();
                    return;
                }

                if (TryLoadFilesFromClipboard())
                {
                    evt_.StopPropagation();
                    return;
                }

                string clipboardText = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clipboardText) && ImageHelper.IsImageFilePath(clipboardText))
                {
                    evt_.StopPropagation();
                    LoadImageFromPath(clipboardText);
                }
            }
        }

        private void HandleGlobalKeyDown(KeyDownEvent evt_)
        {
            bool isCtrlV = evt_.ctrlKey && evt_.keyCode == KeyCode.V;
            bool isCmdV = evt_.commandKey && evt_.keyCode == KeyCode.V;

            if (isCtrlV || isCmdV)
            {
                if (evt_.target == _chatPromptField || evt_.target == _systemPromptField)
                {
                    return;
                }

                if (TryLoadImageFromClipboard())
                {
                    evt_.StopPropagation();
                    return;
                }

                if (TryLoadFilesFromClipboard())
                {
                    evt_.StopPropagation();
                    return;
                }

                string clipboardText = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clipboardText) && (ImageHelper.IsImageFilePath(clipboardText) || ImageDragDropHelper.IsSupportedDocumentFile(clipboardText)))
                {
                    evt_.StopPropagation();
                    LoadImageFromPath(clipboardText);
                }
            }
        }

        private bool IsViewVisible()
        {
            return panel != null && parent != null && parent.style.display == DisplayStyle.Flex;
        }

        private void OnGUIHandler()
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.V &&
                (evt.command || evt.control) && IsViewVisible())
            {
                if (TryLoadImageFromClipboard() || TryLoadFilesFromClipboard())
                {
                    evt.Use();
                    return;
                }

                string clipboardText = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrEmpty(clipboardText) && (ImageHelper.IsImageFilePath(clipboardText) || ImageDragDropHelper.IsSupportedDocumentFile(clipboardText)))
                {
                    LoadImageFromPath(clipboardText);
                    evt.Use();
                    return;
                }
            }

            if (!_waitingForObjectPicker)
                return;

            if (evt.commandName == "ObjectSelectorUpdated" &&
                EditorGUIUtility.GetObjectPickerControlID() == _objectPickerControlId)
            {
                Texture2D selected = EditorGUIUtility.GetObjectPickerObject() as Texture2D;
                if (selected != null)
                {
                    AddTextureFromProject(selected);
                    _waitingForObjectPicker = false;
                    _objectPickerControlId = 0;
                }
            }
            else if (evt.commandName == "ObjectSelectorClosed")
            {
                _waitingForObjectPicker = false;
                _objectPickerControlId = 0;
            }
        }

        private bool TryLoadFilesFromClipboard()
        {
            try
            {
                if (!ClipboardHelper.HasFilePaths())
                    return false;

                List<string> filePaths = ClipboardHelper.GetFilePaths();
                if (filePaths == null || filePaths.Count == 0)
                    return false;

                bool loadedAny = false;
                foreach (string path in filePaths)
                {
                    if (ImageHelper.IsImageFilePath(path) || ImageDragDropHelper.IsSupportedDocumentFile(path))
                    {
                        LoadImageFromPath(path);
                        loadedAny = true;
                    }
                }

                return loadedAny;
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to paste files: {ex.Message}");
                return false;
            }
        }

        private bool TryLoadImageFromClipboard()
        {
            try
            {
                if (!ClipboardHelper.HasImage())
                    return false;

                Texture2D texture = ClipboardHelper.GetImage();
                if (texture == null)
                    return false;

                string fileName = $"clipboard_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                AddImageFromTexture(texture, fileName, "image/png");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to paste image: {ex.Message}");
                return false;
            }
        }

        private void LoadImageFromPath(string path_)
        {
            try
            {
                string extension = Path.GetExtension(path_).ToLowerInvariant();

                if (FileAttachmentHelper.IsPdfExtension(extension) || FileAttachmentHelper.IsTextExtension(extension))
                {
                    LoadFileFromPath(path_);
                    return;
                }

                Texture2D texture = ImageHelper.LoadTextureFromFile(path_);
                string fileName = Path.GetFileName(path_);
                string mediaType = ImageHelper.GetMediaType(extension);

                AddImageFromTexture(texture, fileName, mediaType);
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading image: {ex.Message}");
            }
        }

        private void LoadFileFromPath(string path_)
        {
            try
            {
                AttachedFileData fileData = FileAttachmentHelper.LoadFile(path_);
                _attachedFiles.Add(fileData);
                RefreshFilePreviews();
                SetStatus($"File attached: {fileData.FileName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading file: {ex.Message}");
            }
        }

        private void AddTextureFromProject(Texture2D texture_)
        {
            if (texture_ == null)
                return;

            try
            {
                Texture2D readableTexture = CreateReadableTexture(texture_);
                string fileName = texture_.name + ".png";
                AddImageFromTexture(readableTexture, fileName, "image/png", texture_);
            }
            catch (Exception ex)
            {
                SetStatus($"Error adding texture: {ex.Message}");
            }
        }

        private Texture2D CreateReadableTexture(Texture2D source_)
        {
            RenderTexture rt = RenderTexture.GetTemporary(source_.width, source_.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            Graphics.Blit(source_, rt);

            Texture2D readableTexture = new Texture2D(source_.width, source_.height, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, source_.width, source_.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return readableTexture;
        }

        private void AddImageFromTexture(Texture2D texture_, string fileName_, string mediaType_ = "image/png", Texture2D sourceTexture_ = null)
        {
            string base64Data = ImageHelper.EncodeToBase64(texture_);
            Texture2D thumbnail = ImageHelper.CreateThumbnail(texture_, 50, 50);

            AttachedFileData attachedFile = AttachedFileData.FromImage(base64Data, mediaType_, fileName_, thumbnail);
            attachedFile.SourceTexture = sourceTexture_;

            _attachedFiles.Add(attachedFile);
            RefreshFilePreviews();

            UnityEngine.Object.DestroyImmediate(texture_);

            SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_IMAGE_ATTACHED, fileName_));
        }

        private void RefreshFilePreviews()
        {
            if (_imagePreviewContainer == null)
                return;

            _imagePreviewContainer.Clear();

            for (int i = 0; i < _attachedFiles.Count; i++)
            {
                int index = i;
                AttachedFileData attachedFile = _attachedFiles[i];

                VisualElement item = new VisualElement();
                item.AddToClassList("attached-image-item");

                if (attachedFile.IsImage && attachedFile.Thumbnail != null)
                {
                    Image thumbnail = new Image();
                    thumbnail.image = attachedFile.Thumbnail;
                    thumbnail.AddToClassList("attached-image-thumbnail");
                    thumbnail.scaleMode = ScaleMode.ScaleToFit;
                    item.Add(thumbnail);
                }
                else
                {
                    VisualElement fileIcon = new VisualElement();
                    fileIcon.AddToClassList("attached-file-icon");
                    fileIcon.AddToClassList(attachedFile.IsPdf ? "file-icon-pdf" : "file-icon-text");

                    Label iconLabel = new Label(attachedFile.IsPdf ? "PDF" : "TXT");
                    iconLabel.AddToClassList("file-icon-label");
                    fileIcon.Add(iconLabel);

                    item.Add(fileIcon);
                }

                if (attachedFile.IsImage && attachedFile.SourceTexture != null)
                {
                    ObjectField textureField = new ObjectField();
                    textureField.objectType = typeof(Texture2D);
                    textureField.value = attachedFile.SourceTexture;
                    textureField.AddToClassList("attached-image-objectfield");
                    textureField.RegisterValueChangedCallback(evt =>
                    {
                        Texture2D newTexture = evt.newValue as Texture2D;
                        if (newTexture == null)
                        {
                            RemoveAttachedFile(index);
                        }
                        else if (newTexture != attachedFile.SourceTexture)
                        {
                            UpdateAttachedImage(index, newTexture);
                        }
                    });
                    item.Add(textureField);
                }
                else
                {
                    VisualElement fileInfo = new VisualElement();
                    fileInfo.AddToClassList("attached-file-info");

                    Label fileNameLabel = new Label(attachedFile.FileName);
                    fileNameLabel.AddToClassList("attached-image-filename");
                    fileInfo.Add(fileNameLabel);

                    string sizeDisplay = attachedFile.GetDisplaySize();
                    if (!string.IsNullOrEmpty(sizeDisplay))
                    {
                        Label sizeLabel = new Label(sizeDisplay);
                        sizeLabel.AddToClassList("attached-file-size");
                        fileInfo.Add(sizeLabel);
                    }

                    item.Add(fileInfo);
                }

                Button removeButton = new Button(() => RemoveAttachedFile(index));
                removeButton.text = "\u00d7";
                removeButton.AddToClassList("attached-image-remove");
                item.Add(removeButton);

                _imagePreviewContainer.Add(item);
            }

            bool hasFiles = _attachedFiles.Count > 0;
            _imagePreviewContainer.style.display = hasFiles ? DisplayStyle.Flex : DisplayStyle.None;

            if (_imageAttachmentContainer != null)
            {
                if (hasFiles)
                {
                    _imageAttachmentContainer.AddToClassList("image-attachment-container--visible");
                }
                else
                {
                    _imageAttachmentContainer.RemoveFromClassList("image-attachment-container--visible");
                }
            }

            if (_textureDropArea != null)
            {
                _textureDropArea.style.display = hasFiles ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_clearImagesButton != null)
                _clearImagesButton.style.display = hasFiles ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RemoveAttachedFile(int index_)
        {
            if (index_ >= 0 && index_ < _attachedFiles.Count)
            {
                AttachedFileData removed = _attachedFiles[index_];
                removed.DisposeThumbnail();

                _attachedFiles.RemoveAt(index_);
                RefreshFilePreviews();

                SetStatus(LocalizationManager.Get(LocalizationKeys.CHAT_IMAGE_REMOVED));
            }
        }

        private void UpdateAttachedImage(int index_, Texture2D newTexture_)
        {
            if (index_ < 0 || index_ >= _attachedFiles.Count || newTexture_ == null)
                return;

            AttachedFileData oldFile = _attachedFiles[index_];
            if (!oldFile.IsImage)
                return;

            try
            {
                oldFile.DisposeThumbnail();

                Texture2D readableTexture = CreateReadableTexture(newTexture_);
                string base64Data = ImageHelper.EncodeToBase64(readableTexture);
                Texture2D thumbnail = ImageHelper.CreateThumbnail(readableTexture, 50, 50);

                oldFile.Base64Data = base64Data;
                oldFile.MediaType = "image/png";
                oldFile.FileName = newTexture_.name + ".png";
                oldFile.Thumbnail = thumbnail;
                oldFile.SourceTexture = newTexture_;

                UnityEngine.Object.DestroyImmediate(readableTexture);

                RefreshFilePreviews();
                SetStatus($"Image updated: {oldFile.FileName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error updating image: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_sendButton != null)
                _sendButton.clicked -= OnSendClicked;

            if (_addAttachmentButton != null)
                _addAttachmentButton.clicked -= ShowAttachmentPopup;

            if (_clearImagesButton != null)
                _clearImagesButton.clicked -= HandleClearImagesClicked;

            if (_persistentToggle != null)
                _persistentToggle.UnregisterValueChangedCallback(HandlePersistentToggleChanged);

            if (_resetSessionButton != null)
                _resetSessionButton.clicked -= OnResetSessionClicked;

            if (_chatPromptField != null)
                _chatPromptField.UnregisterCallback<KeyDownEvent>(HandlePromptKeyDown, TrickleDown.TrickleDown);

            this.UnregisterCallback<KeyDownEvent>(HandleGlobalKeyDown, TrickleDown.TrickleDown);

            CloseAttachmentPopup();
            ClearAttachments();
        }
    }
}
