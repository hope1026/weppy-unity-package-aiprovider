using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class BgRemovalInputSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "BgRemoval/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "BgRemovalInputSection/BgRemovalInputSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "BgRemovalInputSection/BgRemovalInputSection.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnProcessRequested;
        public event Action OnProcessAllRequested;
        public event Action OnCancelRequested;
        public event Action OnInputImageChanged;

        private EditorDataStorage _storage;

        private VisualElement _inputImageContainer;
        private VisualElement _inputImagePreview;
        private VisualElement _textureDropArea;
        private Label _dropAreaHint;
        private Label _attachmentHint;
        private Button _processButton;
        private DropdownField _processModeDropdown;
        private Button _addImageButton;
        private Button _addProjectTextureButton;
        private Button _clearImageButton;

        private VisualElement _loadingContainer;
        private Label _loadingLabel;
        private Button _cancelButton;

        private AttachedImageData _inputImage;
        private IMGUIContainer _imguiContainer;
        private static int _objectPickerControlId = 0;
        private bool _waitingForObjectPicker = false;

        public BgRemovalInputSection()
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

        public void Initialize(EditorDataStorage storage_)
        {
            _storage = storage_;
            UpdateLocalization();
        }

        private void SetupUI()
        {
            _inputImageContainer = this.Q<VisualElement>("input-image-container");
            _inputImagePreview = this.Q<VisualElement>("input-image-preview");
            _textureDropArea = this.Q<VisualElement>("texture-drop-area");
            _dropAreaHint = _textureDropArea?.Q<Label>(className: "drop-area-hint");
            _attachmentHint = this.Q<Label>("attachment-hint");

            _processButton = this.Q<Button>("process-button");
            _processModeDropdown = this.Q<DropdownField>("process-mode-dropdown");
            _addImageButton = this.Q<Button>("add-image-button");
            _addProjectTextureButton = this.Q<Button>("add-project-texture-button");
            _clearImageButton = this.Q<Button>("clear-image-button");

            if (_processButton != null)
            {
                _processButton.clicked += HandleProcessClicked;
            }

            if (_processModeDropdown != null)
            {
                _processModeDropdown.choices = new List<string>
                {
                    LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PROCESS_MODE_PRIORITY),
                    LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PROCESS_MODE_ALL)
                };
                _processModeDropdown.index = 0;
                _processModeDropdown.RegisterValueChangedCallback(_ => UpdateProcessButtonTooltip());
            }
            if (_addImageButton != null)
            {
                _addImageButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_ADD_IMAGE);
                _addImageButton.clicked += HandleAddImageClicked;
            }
            if (_addProjectTextureButton != null)
            {
                _addProjectTextureButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_ADD_PROJECT_TEXTURE);
                _addProjectTextureButton.clicked += HandleAddProjectTextureClicked;
            }
            if (_clearImageButton != null)
            {
                _clearImageButton.clicked += HandleClearImageClicked;
            }

            CreateLoadingIndicator();
            SetupDragAndDrop();
            SetupTextureDrop();
            SetupIMGUIContainer();

            this.RegisterCallback<KeyDownEvent>(HandleGlobalKeyDown, TrickleDown.TrickleDown);
            this.focusable = true;

            RefreshInputImagePreview();
        }

        public void UpdateLocalization()
        {
            UpdateProcessModeOptions();

            if (_dropAreaHint != null)
            {
                _dropAreaHint.text = LocalizationManager.Get(LocalizationKeys.BGREMOVAL_DROP_AREA_HINT);
            }
            if (_attachmentHint != null)
            {
                _attachmentHint.text = LocalizationManager.Get(LocalizationKeys.BGREMOVAL_ATTACHMENT_HINT);
            }
        }

        private void UpdateProcessModeOptions()
        {
            if (_processModeDropdown != null)
            {
                int currentIndex = _processModeDropdown.index;
                _processModeDropdown.choices = new List<string>
                {
                    LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PROCESS_MODE_PRIORITY),
                    LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PROCESS_MODE_ALL)
                };
                _processModeDropdown.index = currentIndex >= 0 ? currentIndex : 0;
            }

            UpdateProcessButtonTooltip();
        }

        private void UpdateProcessButtonTooltip()
        {
            if (_processButton == null)
                return;

            bool isAllMode = _processModeDropdown != null && _processModeDropdown.index == 1;
            _processButton.tooltip = LocalizationManager.Get(isAllMode
                ? LocalizationKeys.BGREMOVAL_PROCESS_ALL_TOOLTIP
                : LocalizationKeys.BGREMOVAL_PROCESS_TOOLTIP);
        }

        public void RegisterDisabledWarning(Action onDisabledClick_)
        {
            WrapButtonForDisabledFeedback(_processButton, onDisabledClick_);
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
            wrapper.style.flexDirection = FlexDirection.Row;

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

            _loadingLabel = new Label(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_PROCESSING));
            _loadingLabel.AddToClassList("loading-label");

            _cancelButton = new Button(HandleCancelClicked);
            _cancelButton.text = LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL);
            _cancelButton.AddToClassList("cancel-button");

            _loadingContainer.Add(_loadingLabel);
            _loadingContainer.Add(_cancelButton);

            VisualElement buttonsContainer = _processButton?.parent;
            if (buttonsContainer != null)
            {
                buttonsContainer.Add(_loadingContainer);
            }
            else
            {
                Add(_loadingContainer);
            }
        }

        public void SetProcessing(bool isProcessing_, string message_ = "Processing...")
        {
            if (_loadingContainer != null)
                _loadingContainer.style.display = isProcessing_ ? DisplayStyle.Flex : DisplayStyle.None;

            if (_loadingLabel != null)
                _loadingLabel.text = message_;
        }

        public void SetActionButtonsEnabled(bool canAddImage_, bool canProcess_)
        {
            if (_processButton != null)
                _processButton.SetEnabled(canProcess_);
            if (_processModeDropdown != null)
                _processModeDropdown.SetEnabled(canProcess_);
            if (_addImageButton != null)
                _addImageButton.SetEnabled(canAddImage_);
            if (_addProjectTextureButton != null)
                _addProjectTextureButton.SetEnabled(canAddImage_);
        }

        public bool HasInputImage()
        {
            return _inputImage != null;
        }

        public AttachedImageData GetInputImage()
        {
            return _inputImage;
        }

        private void HandleProcessClicked()
        {
            bool isAllMode = _processModeDropdown != null && _processModeDropdown.index == 1;
            if (isAllMode)
            {
                OnProcessAllRequested?.Invoke();
            }
            else
            {
                OnProcessRequested?.Invoke();
            }
        }

        private void HandleCancelClicked()
        {
            OnCancelRequested?.Invoke();
        }

        private void HandleClearImageClicked()
        {
            ClearInputImage();
            SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_IMAGE_CLEARED));
        }

        private void SetupDragAndDrop()
        {
            ImageDragDropHelper.SetupFileDragAndDropSingle(
                _inputImageContainer,
                LoadImageFromPath
            );
        }

        private void SetupTextureDrop()
        {
            ImageDragDropHelper.SetupTextureDragAndDropSingle(
                _textureDropArea,
                AddTextureFromProject
            );
        }

        private void HandleAddImageClicked()
        {
            string lastPath = _storage != null
                ? _storage.GetString(EditorDataStorageKeys.KEY_LAST_BGREMOVAL_INPUT_PATH, "")
                : "";
            string initialDirectory = string.IsNullOrEmpty(lastPath) ? "" : lastPath;
            string path = EditorUtility.OpenFilePanel(
                LocalizationManager.Get(LocalizationKeys.BGREMOVAL_SELECT_IMAGE),
                initialDirectory,
                "png,jpg,jpeg,gif,webp"
            );

            if (!string.IsNullOrEmpty(path))
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    _storage?.SetString(EditorDataStorageKeys.KEY_LAST_BGREMOVAL_INPUT_PATH, directory);
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

        private void HandleGlobalKeyDown(KeyDownEvent evt_)
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
                SetInputImage(texture, fileName, "image/png");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to paste image: {ex.Message}");
                return false;
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

                foreach (string path in filePaths)
                {
                    if (ImageHelper.IsImageFilePath(path))
                    {
                        LoadImageFromPath(path);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                SetStatus($"Failed to paste files: {ex.Message}");
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

                SetInputImage(texture, fileName, mediaType);
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
                SetInputImage(readableTexture, fileName, "image/png", texture_);
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

        private void SetInputImage(Texture2D texture_, string fileName_, string mediaType_, Texture2D sourceTexture_ = null)
        {
            ClearInputImage();

            string base64Data = ImageHelper.EncodeToBase64(texture_);
            Texture2D thumbnail = ImageHelper.CreateThumbnail(texture_, 512, 512);

            _inputImage = new AttachedImageData(base64Data, mediaType_, fileName_, thumbnail);
            _inputImage.SourceTexture = sourceTexture_;

            RefreshInputImagePreview();
            OnInputImageChanged?.Invoke();

            if (sourceTexture_ == null)
                UnityEngine.Object.DestroyImmediate(texture_);

            SetStatus(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_IMAGE_SET, fileName_));
        }

        private void RefreshInputImagePreview()
        {
            if (_inputImagePreview == null)
                return;

            _inputImagePreview.Clear();

            if (_inputImage == null)
            {
                _inputImagePreview.AddToClassList("hidden");
                return;
            }

            _inputImagePreview.RemoveFromClassList("hidden");

            if (_inputImage.SourceTexture != null)
            {
                // Create a wrapper with checkerboard background for transparency visibility
                VisualElement imageWrapper = new VisualElement();
                imageWrapper.AddToClassList("preview-image-wrapper");

                // Calculate size maintaining aspect ratio
                float maxDimension = 300f;
                float width = _inputImage.SourceTexture.width;
                float height = _inputImage.SourceTexture.height;
                float aspect = width / height;

                if (width > maxDimension || height > maxDimension)
                {
                    if (width > height)
                    {
                        width = maxDimension;
                        height = width / aspect;
                    }
                    else
                    {
                        height = maxDimension;
                        width = height * aspect;
                    }
                }

                imageWrapper.style.width = width;
                imageWrapper.style.height = height;

                // Add checkerboard background
                UnityEngine.UIElements.Image checkerboard = new UnityEngine.UIElements.Image();
                checkerboard.image = CreateCheckerboardTexture();
                checkerboard.scaleMode = ScaleMode.ScaleAndCrop;
                checkerboard.AddToClassList("checkerboard-background");
                checkerboard.style.position = Position.Absolute;
                checkerboard.style.width = Length.Percent(100);
                checkerboard.style.height = Length.Percent(100);
                imageWrapper.Add(checkerboard);

                // Add actual image on top
                UnityEngine.UIElements.Image previewImage = new UnityEngine.UIElements.Image();
                previewImage.image = _inputImage.SourceTexture;
                previewImage.AddToClassList("preview-image");
                previewImage.scaleMode = ScaleMode.ScaleToFit;
                previewImage.style.position = Position.Absolute;
                previewImage.style.width = Length.Percent(100);
                previewImage.style.height = Length.Percent(100);
                imageWrapper.Add(previewImage);

                _inputImagePreview.Add(imageWrapper);

                Label fileNameLabel = new Label(_inputImage.FileName);
                fileNameLabel.AddToClassList("attached-image-filename");
                _inputImagePreview.Add(fileNameLabel);
            }
        }

        private void ClearInputImage()
        {
            if (_inputImage != null)
            {
                _inputImage.DisposeThumbnail();
                _inputImage = null;
            }
            RefreshInputImagePreview();
            OnInputImageChanged?.Invoke();
        }

        private Texture2D CreateCheckerboardTexture(int tileSize_ = 16)
        {
            int size = tileSize_ * 2;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color lightGray = new Color(0.8f, 0.8f, 0.8f, 1f);
            Color darkGray = new Color(0.6f, 0.6f, 0.6f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isLightTile = ((x / tileSize_) + (y / tileSize_)) % 2 == 0;
                    texture.SetPixel(x, y, isLightTile ? lightGray : darkGray);
                }
            }

            texture.Apply();
            return texture;
        }

        public void Dispose()
        {
            if (_processButton != null)
                _processButton.clicked -= HandleProcessClicked;
            if (_cancelButton != null)
                _cancelButton.clicked -= HandleCancelClicked;
            if (_addImageButton != null)
                _addImageButton.clicked -= HandleAddImageClicked;
            if (_addProjectTextureButton != null)
                _addProjectTextureButton.clicked -= HandleAddProjectTextureClicked;
            if (_clearImageButton != null)
                _clearImageButton.clicked -= HandleClearImageClicked;

            this.UnregisterCallback<KeyDownEvent>(HandleGlobalKeyDown, TrickleDown.TrickleDown);

            ClearInputImage();
        }
    }
}