using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ImageRequestSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "Image/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "ImageRequestSection/ImageRequestSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "ImageRequestSection/ImageRequestSection.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnGenerateRequested;
        public event Action OnGenerateAllRequested;
        public event Action OnCancelRequested;

        private EditorProviderManagerImage _editorProviderManagerImageRef;
        private EditorDataStorage _storageRef;

        private TextField _imagePromptField;
        private TextField _negativePromptField;
        private VisualElement _dynamicOptionsContainer;
        private Foldout _dynamicOptionsFoldout;
        private Label _optionsHintLabel;
        private ImageDynamicOptionRenderer _optionRenderer;
        private Button _generateButton;
        private DropdownField _generateModeDropdown;

        private VisualElement _loadingContainer;
        private Label _loadingLabel;
        private Button _cancelButton;

        private VisualElement _imageAttachmentContainer;
        private VisualElement _imagePreviewList;
        private VisualElement _textureDropArea;
        private Label _dropAreaHint;
        private Label _attachmentHint;
        private Button _addAttachmentButton;
        private Button _addImageButton;
        private Button _addProjectTextureButton;
        private Button _clearImagesButton;
        private VisualElement _attachmentPopup;
        private List<AttachedImageData> _attachedImages = new List<AttachedImageData>();
        private IMGUIContainer _imguiContainer;

        private static int _objectPickerControlId = 0;
        private bool _waitingForObjectPicker = false;
        private Action _onDisabledClick;

        public ImageRequestSection()
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

        public void Initialize(EditorProviderManagerImage editorProviderManager_ , EditorDataStorage storage_, string languageCode_)
        {
            _editorProviderManagerImageRef = editorProviderManager_;
            _storageRef = storage_;
            if (_dynamicOptionsContainer != null)
            {
                _optionRenderer = new ImageDynamicOptionRenderer(_dynamicOptionsContainer, languageCode_);
            }
            ApplyDynamicOptionsCollapseState();
            UpdateLocalization();
        }

        private void SetupUI()
        {
            _imagePromptField = this.Q<TextField>("image-prompt-field");
            _negativePromptField = this.Q<TextField>("negative-prompt-field");
            _dynamicOptionsContainer = this.Q<VisualElement>("dynamic-options-container");
            _dynamicOptionsFoldout = this.Q<Foldout>("dynamic-options-foldout");

            if (_dynamicOptionsFoldout != null)
            {
                Toggle toggle = _dynamicOptionsFoldout.Q<Toggle>();
                if (toggle != null)
                {
                    _optionsHintLabel = new Label();
                    _optionsHintLabel.AddToClassList("options-hint-label");
                    toggle.Add(_optionsHintLabel);
                }
            }

            _generateButton = this.Q<Button>("generate-image-button");
            _generateModeDropdown = this.Q<DropdownField>("generate-mode-dropdown");

            _addAttachmentButton = this.Q<Button>("add-attachment-button");

            if (_generateButton != null)
            {
                _generateButton.clicked += HandleGenerateClicked;
            }

            if (_generateModeDropdown != null)
            {
                _generateModeDropdown.choices = new List<string>
                {
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_SEND_MODE_PRIORITY),
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_SEND_MODE_ALL)
                };
                _generateModeDropdown.index = 0;
                _generateModeDropdown.RegisterValueChangedCallback(_ => UpdateGenerateButtonTooltip());
            }

            if (_dynamicOptionsFoldout != null)
            {
                _dynamicOptionsFoldout.RegisterValueChangedCallback(HandleDynamicOptionsFoldoutChanged);
            }

            CreateLoadingIndicator();

            _imageAttachmentContainer = this.Q<VisualElement>("image-attachment-container");
            _imagePreviewList = this.Q<VisualElement>("image-preview-list");
            _textureDropArea = this.Q<VisualElement>("texture-drop-area");
            _dropAreaHint = _textureDropArea?.Q<Label>(className: "drop-area-hint");
            _attachmentHint = this.Q<Label>("attachment-hint");
            _clearImagesButton = this.Q<Button>("clear-images-button");

            if (_addAttachmentButton != null)
            {
                _addAttachmentButton.clicked += ShowAttachmentPopup;
            }

            if (_clearImagesButton != null)
                _clearImagesButton.clicked += HandleClearImagesClicked;

            if (_imagePromptField != null)
                _imagePromptField.RegisterCallback<KeyDownEvent>(HandlePromptKeyDown, TrickleDown.TrickleDown);

            this.RegisterCallback<KeyDownEvent>(HandleGlobalKeyDown, TrickleDown.TrickleDown);
            this.focusable = true;

            SetupDragAndDrop();
            SetupTextureDrop();
            SetupIMGUIContainer();
            RefreshImagePreviews();
        }

        public void UpdateLocalization()
        {
            UpdateGenerateModeOptions();

            if (_imagePromptField != null)
            {
                _imagePromptField.tooltip = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_PROMPT_TOOLTIP);
            }

            if (_negativePromptField != null)
            {
                _negativePromptField.tooltip = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NEGATIVE_PROMPT_TOOLTIP);
            }

            if (_dropAreaHint != null)
            {
                _dropAreaHint.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_DROP_AREA_HINT);
            }

            if (_attachmentHint != null)
            {
                _attachmentHint.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_ATTACHMENT_HINT);
            }

            if (_dynamicOptionsFoldout != null)
            {
                _dynamicOptionsFoldout.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_OPTIONS_LABEL);
            }

            if (_optionsHintLabel != null)
            {
                _optionsHintLabel.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_OPTIONS_HINT);
            }

            if (_addAttachmentButton != null)
            {
                _addAttachmentButton.tooltip = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_ADD_ATTACHMENT_TOOLTIP);
            }
        }

        private void UpdateGenerateModeOptions()
        {
            if (_generateModeDropdown != null)
            {
                int currentIndex = _generateModeDropdown.index;
                _generateModeDropdown.choices = new List<string>
                {
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_SEND_MODE_PRIORITY),
                    LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_SEND_MODE_ALL)
                };
                _generateModeDropdown.index = currentIndex >= 0 ? currentIndex : 0;
            }

            UpdateGenerateButtonTooltip();
        }

        private void UpdateGenerateButtonTooltip()
        {
            if (_generateButton == null)
                return;

            bool isAllMode = _generateModeDropdown != null && _generateModeDropdown.index == 1;
            _generateButton.tooltip = LocalizationManager.Get(isAllMode
                ? LocalizationKeys.IMAGE_GENERATION_GENERATE_ALL_TOOLTIP
                : LocalizationKeys.IMAGE_GENERATION_GENERATE_PRIORITY_TOOLTIP);
        }

        public void RegisterDisabledWarning(Action onDisabledClick_)
        {
            _onDisabledClick = onDisabledClick_;
            WrapButtonForDisabledFeedback(_generateButton, onDisabledClick_);
            WrapButtonForDisabledFeedback(_addAttachmentButton, onDisabledClick_);
            WrapButtonForDisabledFeedback(_addImageButton, onDisabledClick_);
            WrapButtonForDisabledFeedback(_addProjectTextureButton, onDisabledClick_);
        }

        private void WrapButtonForDisabledFeedback(Button button_, Action onDisabledClick_)
        {
            if (button_ == null)
                return;

            VisualElement buttonParent = button_.parent;
            if (buttonParent == null)
                return;

            int index = buttonParent.IndexOf(button_);

            VisualElement wrapper = new VisualElement();
            wrapper.AddToClassList("button-wrapper");

            buttonParent.Remove(button_);
            wrapper.Add(button_);
            buttonParent.Insert(index, wrapper);

            wrapper.RegisterCallback<ClickEvent>(evt_ =>
            {
                if (!button_.enabledSelf)
                {
                    onDisabledClick_?.Invoke();
                    evt_.StopPropagation();
                }
            });
        }

        private void CreateLoadingIndicator()
        {
            _loadingContainer = new VisualElement();
            _loadingContainer.AddToClassList("loading-container");
            _loadingContainer.style.display = DisplayStyle.None;

            _loadingLabel = new Label(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_GENERATING));
            _loadingLabel.AddToClassList("loading-label");

            _cancelButton = new Button(HandleCancelClicked);
            _cancelButton.text = LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL);
            _cancelButton.AddToClassList("cancel-button");

            _loadingContainer.Add(_loadingLabel);
            _loadingContainer.Add(_cancelButton);

            VisualElement buttonsContainer = _generateButton?.parent;
            if (buttonsContainer != null)
            {
                buttonsContainer.Add(_loadingContainer);
            }
            else
            {
                Add(_loadingContainer);
            }
        }

        public void SetGenerating(bool isGenerating_, string message_ = "Generating...")
        {
            if (_loadingContainer != null)
                _loadingContainer.style.display = isGenerating_ ? DisplayStyle.Flex : DisplayStyle.None;

            if (_loadingLabel != null)
                _loadingLabel.text = message_;
        }

        public void SetActionButtonsEnabled(bool enabled_)
        {
            if (_generateButton != null)
                _generateButton.SetEnabled(enabled_);
            if (_generateModeDropdown != null)
                _generateModeDropdown.SetEnabled(enabled_);
            if (_addAttachmentButton != null)
                _addAttachmentButton.SetEnabled(enabled_);
        }

        public string GetPrompt()
        {
            return _imagePromptField?.value ?? string.Empty;
        }

        public string GetNegativePrompt()
        {
            return _negativePromptField?.value ?? string.Empty;
        }

        public Dictionary<string, string> GetSelectedOptions()
        {
            if (_optionRenderer == null)
                return new Dictionary<string, string>();

            return _optionRenderer.GetSelectedValues();
        }

        public IReadOnlyList<AttachedImageData> GetAttachedImages()
        {
            return _attachedImages;
        }

        public void RefreshDynamicOptions(IReadOnlyList<ImageModelInfo> activeModels_)
        {
            if (_optionRenderer == null)
                return;

            _optionRenderer.RenderOptions(activeModels_);
        }

        private void SetupDragAndDrop()
        {
            ImageDragDropHelper.SetupFileDragAndDrop(
                _imageAttachmentContainer,
                LoadImageFromPath
            );
        }

        private void SetupTextureDrop()
        {
            ImageDragDropHelper.SetupTextureDragAndDrop(
                _textureDropArea,
                AddTextureFromProject
            );
        }

        private void ShowAttachmentPopup()
        {
            if (_attachmentPopup != null)
            {
                CloseAttachmentPopup();
                return;
            }

            _attachmentPopup = CreateAttachmentPopup();
            Add(_attachmentPopup);

            _attachmentPopup.style.position = Position.Absolute;

            if (_addAttachmentButton != null)
            {
                Rect buttonRect = _addAttachmentButton.worldBound;
                Rect thisRect = worldBound;

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

            Button addImageButton = new Button(() =>
            {
                CloseAttachmentPopup();
                HandleAddImageClicked();
            });
            addImageButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_ADD_IMAGE);
            addImageButton.AddToClassList("attachment-popup-item");
            popup.Add(addImageButton);

            Button addTextureButton = new Button(() =>
            {
                CloseAttachmentPopup();
                HandleAddProjectTextureClicked();
            });
            addTextureButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_ADD_PROJECT_TEXTURE);
            addTextureButton.AddToClassList("attachment-popup-item");
            popup.Add(addTextureButton);

            _addImageButton = addImageButton;
            _addProjectTextureButton = addTextureButton;

            if (_onDisabledClick != null)
            {
                WrapButtonForDisabledFeedback(addImageButton, _onDisabledClick);
                WrapButtonForDisabledFeedback(addTextureButton, _onDisabledClick);
            }

            popup.RegisterCallback<FocusOutEvent>(_ =>
            {
                popup.schedule.Execute(CloseAttachmentPopup).ExecuteLater(100);
            });

            return popup;
        }

        private void HandleAddImageClicked()
        {
            string lastPath = _storageRef != null
                ? _storageRef.GetString(EditorDataStorageKeys.KEY_LAST_IMAGE_INPUT_PATH, "")
                : "";
            string initialDirectory = string.IsNullOrEmpty(lastPath) ? "" : lastPath;
            string path = EditorUtility.OpenFilePanel(
                LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_SELECT_IMAGE),
                initialDirectory,
                "png,jpg,jpeg,gif,webp"
            );

            if (!string.IsNullOrEmpty(path))
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    _storageRef?.SetString(EditorDataStorageKeys.KEY_LAST_IMAGE_INPUT_PATH, directory);
                }
                LoadImageFromPath(path);
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

        private void HandleAddProjectTextureClicked()
        {
            _objectPickerControlId = GUIUtility.GetControlID(FocusType.Passive) + 100;
            _waitingForObjectPicker = true;
            EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", _objectPickerControlId);
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
                if (!string.IsNullOrEmpty(clipboardText) && ImageHelper.IsImageFilePath(clipboardText))
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

        private void HandleClearImagesClicked()
        {
            if (_attachedImages.Count > 0)
            {
                ClearAttachedImages();
                SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_REFS_CLEARED));
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
                if (evt_.target == _imagePromptField || evt_.target == _negativePromptField)
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
                if (!string.IsNullOrEmpty(clipboardText) && ImageHelper.IsImageFilePath(clipboardText))
                {
                    evt_.StopPropagation();
                    LoadImageFromPath(clipboardText);
                }
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
                    if (ImageHelper.IsImageFilePath(path))
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
                Texture2D texture = ImageHelper.LoadTextureFromFile(path_);
                string fileName = Path.GetFileName(path_);
                string extension = Path.GetExtension(path_);
                string mediaType = ImageHelper.GetMediaType(extension);

                AddImageFromTexture(texture, fileName, mediaType);
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading image: {ex.Message}");
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
            Texture2D thumbnail = ImageHelper.CreateThumbnail(texture_, 80, 80);

            AttachedImageData attachedImage = new AttachedImageData(base64Data, mediaType_, fileName_, thumbnail);
            attachedImage.SourceTexture = sourceTexture_;

            _attachedImages.Add(attachedImage);
            RefreshImagePreviews();

            UnityEngine.Object.DestroyImmediate(texture_);

            SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_REF_ATTACHED, fileName_));
        }

        private void RefreshImagePreviews()
        {
            if (_imagePreviewList == null)
                return;

            _imagePreviewList.Clear();

            for (int i = 0; i < _attachedImages.Count; i++)
            {
                int index = i;
                AttachedImageData attachedImage = _attachedImages[i];

                VisualElement item = new VisualElement();
                item.AddToClassList("attached-image-item");

                UnityEngine.UIElements.Image thumbnail = new UnityEngine.UIElements.Image();
                thumbnail.image = attachedImage.Thumbnail;
                thumbnail.AddToClassList("attached-image-thumbnail");
                thumbnail.scaleMode = ScaleMode.ScaleToFit;
                item.Add(thumbnail);

                if (attachedImage.SourceTexture != null)
                {
                    ObjectField textureField = new ObjectField();
                    textureField.objectType = typeof(Texture2D);
                    textureField.value = attachedImage.SourceTexture;
                    textureField.AddToClassList("attached-image-objectfield");
                    textureField.RegisterValueChangedCallback(evt_ =>
                    {
                        Texture2D newTexture = evt_.newValue as Texture2D;
                        if (newTexture == null)
                        {
                            RemoveAttachedImage(index);
                        }
                        else if (newTexture != attachedImage.SourceTexture)
                        {
                            UpdateAttachedImage(index, newTexture);
                        }
                    });
                    item.Add(textureField);
                }
                else
                {
                    Label fileNameLabel = new Label(attachedImage.FileName);
                    fileNameLabel.AddToClassList("attached-image-filename");
                    item.Add(fileNameLabel);
                }

                Button removeButton = new Button(() => RemoveAttachedImage(index));
                removeButton.text = "\u00d7";
                removeButton.AddToClassList("attached-image-remove");
                item.Add(removeButton);

                _imagePreviewList.Add(item);
            }

            bool hasImages = _attachedImages.Count > 0;
            _imagePreviewList.style.display = hasImages ? DisplayStyle.Flex : DisplayStyle.None;

            if (_imageAttachmentContainer != null)
            {
                if (hasImages)
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
                _textureDropArea.style.display = hasImages ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_clearImagesButton != null)
                _clearImagesButton.style.display = hasImages ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RemoveAttachedImage(int index_)
        {
            if (index_ >= 0 && index_ < _attachedImages.Count)
            {
                AttachedImageData removed = _attachedImages[index_];
                removed.DisposeThumbnail();

                _attachedImages.RemoveAt(index_);
                RefreshImagePreviews();

                SetStatus(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_REF_REMOVED));
            }
        }

        private void UpdateAttachedImage(int index_, Texture2D newTexture_)
        {
            if (index_ < 0 || index_ >= _attachedImages.Count || newTexture_ == null)
                return;

            try
            {
                AttachedImageData oldImage = _attachedImages[index_];
                oldImage.DisposeThumbnail();

                Texture2D readableTexture = CreateReadableTexture(newTexture_);
                string base64Data = ImageHelper.EncodeToBase64(readableTexture);
                Texture2D thumbnail = ImageHelper.CreateThumbnail(readableTexture, 80, 80);

                oldImage.Base64Data = base64Data;
                oldImage.MediaType = "image/png";
                oldImage.FileName = newTexture_.name + ".png";
                oldImage.Thumbnail = thumbnail;
                oldImage.SourceTexture = newTexture_;

                UnityEngine.Object.DestroyImmediate(readableTexture);

                RefreshImagePreviews();
                SetStatus($"Reference image updated: {oldImage.FileName}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error updating image: {ex.Message}");
            }
        }

        private void ClearAttachedImages()
        {
            foreach (AttachedImageData attachedImage in _attachedImages)
            {
                attachedImage.DisposeThumbnail();
            }

            _attachedImages.Clear();
            RefreshImagePreviews();
        }

        public void Dispose()
        {
            if (_generateButton != null)
                _generateButton.clicked -= HandleGenerateClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= HandleCancelClicked;

            if (_addAttachmentButton != null)
                _addAttachmentButton.clicked -= ShowAttachmentPopup;

            if (_clearImagesButton != null)
                _clearImagesButton.clicked -= HandleClearImagesClicked;

            if (_imagePromptField != null)
                _imagePromptField.UnregisterCallback<KeyDownEvent>(HandlePromptKeyDown, TrickleDown.TrickleDown);

            this.UnregisterCallback<KeyDownEvent>(HandleGlobalKeyDown, TrickleDown.TrickleDown);

            if (_dynamicOptionsFoldout != null)
                _dynamicOptionsFoldout.UnregisterValueChangedCallback(HandleDynamicOptionsFoldoutChanged);

            CloseAttachmentPopup();
            ClearAttachedImages();
        }

        private void HandleDynamicOptionsFoldoutChanged(ChangeEvent<bool> evt_)
        {
            if (_storageRef == null)
                return;

            bool isCollapsed = !evt_.newValue;
            _storageRef.SetBool(EditorDataStorageKeys.KEY_IMAGE_DYNAMIC_OPTIONS_COLLAPSED, isCollapsed);
        }

        private void ApplyDynamicOptionsCollapseState()
        {
            if (_dynamicOptionsFoldout == null)
                return;

            bool isCollapsed = _storageRef != null && _storageRef.GetBool(EditorDataStorageKeys.KEY_IMAGE_DYNAMIC_OPTIONS_COLLAPSED, false);
            _dynamicOptionsFoldout.value = !isCollapsed;
        }

        private void HandleGenerateClicked()
        {
            bool isAllMode = _generateModeDropdown != null && _generateModeDropdown.index == 1;
            if (isAllMode)
            {
                OnGenerateAllRequested?.Invoke();
            }
            else
            {
                OnGenerateRequested?.Invoke();
            }
        }

        private void HandleCancelClicked()
        {
            OnCancelRequested?.Invoke();
        }
    }
}
