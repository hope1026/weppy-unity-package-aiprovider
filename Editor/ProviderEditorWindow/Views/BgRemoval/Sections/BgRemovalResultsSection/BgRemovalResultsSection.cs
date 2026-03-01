using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class BgRemovalResultsSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "BgRemoval/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "BgRemovalResultsSection/BgRemovalResultsSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "BgRemovalResultsSection/BgRemovalResultsSection.uss";

        private class BgRemovalResultData
        {
            public string ProviderName { get; set; }
            public string Timestamp { get; set; }
            public bool IsSuccess { get; set; }
            public string ErrorMessage { get; set; }
            public Texture2D Texture { get; set; }
            public Texture2D OriginalTexture { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public long FileSize { get; set; }
        }

        public event Action<string> OnStatusChanged;

        private VisualElement _resultsContainer;
        private Button _clearButton;
        private DropdownField _historyDropdown;
        private Button _historyLoadButton;
        private Label _historyLabel;
        private List<HistoryEntryInfo> _historyEntries = new List<HistoryEntryInfo>();
        private List<BgRemovalResultData> _resultsData = new List<BgRemovalResultData>();
        private bool _suppressHistorySave;

        public BgRemovalResultsSection()
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
            _resultsContainer = this.Q<VisualElement>("results-container");
            _clearButton = this.Q<Button>("clear-result-button");
            _historyDropdown = this.Q<DropdownField>("history-dropdown");
            _historyLoadButton = this.Q<Button>("history-load-button");
            _historyLabel = this.Q<Label>("history-label");

            if (_clearButton != null)
            {
                _clearButton.clicked += ClearResults;
            }

            if (_historyLoadButton != null)
            {
                _historyLoadButton.clicked += OnHistoryLoadClicked;
            }

            UpdateHistoryUI();
            RefreshHistoryList();
        }

        public void ClearResults()
        {
            if (!_suppressHistorySave)
            {
                SaveHistoryIfNeeded();
            }

            foreach (BgRemovalResultData resultData in _resultsData)
            {
                if (resultData.Texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(resultData.Texture);
                }
                if (resultData.OriginalTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(resultData.OriginalTexture);
                }
            }

            _resultsContainer?.Clear();
            _resultsData.Clear();
        }

        public void AddResult(string providerName_, BgRemovalResponse response_, Texture2D originalTexture_ = null)
        {
            // Ensure this method runs on the main thread
            if (!MainThreadDispatcher.IsMainThread())
            {
                MainThreadDispatcher.Dispatch(() => AddResult(providerName_, response_, originalTexture_));
                return;
            }

            VisualElement resultEntry = new VisualElement();
            resultEntry.AddToClassList("result-entry");

            VisualElement header = new VisualElement();
            header.AddToClassList("result-header");

            Label providerLabel = new Label(providerName_);
            providerLabel.AddToClassList("result-provider-name");

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Label timestampLabel = new Label(timestamp);
            timestampLabel.AddToClassList("result-timestamp");

            Label statusLabel = new Label(response_.IsSuccess ? LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS) : LocalizationManager.Get(LocalizationKeys.COMMON_ERROR));
            statusLabel.AddToClassList("result-status");
            statusLabel.AddToClassList(response_.IsSuccess ? "success" : "error");

            header.Add(providerLabel);
            header.Add(timestampLabel);
            header.Add(statusLabel);
            resultEntry.Add(header);

            if (response_.IsSuccess && response_.HasImage)
            {
                VisualElement imageContainer = new VisualElement();
                imageContainer.AddToClassList("result-image-container");

                byte[] imageBytes = null;
                try
                {
                    imageBytes = Convert.FromBase64String(response_.Base64Image);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AIProvider] Failed to decode image data: {ex.Message}");
                }

                Texture2D texture = CreateTextureFromBytes(imageBytes);

                if (texture != null)
                {
                    // Create before/after comparison container
                    VisualElement comparisonContainer = new VisualElement();
                    comparisonContainer.AddToClassList("before-after-container");

                    // Add original image (Before) if available
                    if (originalTexture_ != null)
                    {
                        VisualElement beforeSection = CreateImageSection(originalTexture_, "Before", false);
                        comparisonContainer.Add(beforeSection);
                    }

                    // Add result image (After)
                    VisualElement afterSection = CreateImageSection(texture, "After", true);
                    comparisonContainer.Add(afterSection);

                    imageContainer.Add(comparisonContainer);

                    Button saveButton = new Button(() => SaveImage(texture, providerName_));
                    saveButton.text = LocalizationManager.Get(LocalizationKeys.BGREMOVAL_SAVE_BUTTON);
                    saveButton.AddToClassList("save-image-button");
                    imageContainer.Add(saveButton);
                }
                else
                {
                    string errorMessage = LocalizationManager.Get(LocalizationKeys.BGREMOVAL_TEXTURE_FAILED);
                    Label errorLabel = new Label(errorMessage);
                    errorLabel.AddToClassList("error-label");
                    imageContainer.Add(errorLabel);

                    BgRemovalResultData resultData = new BgRemovalResultData
                    {
                        ProviderName = providerName_,
                        Timestamp = timestamp,
                        IsSuccess = false,
                        ErrorMessage = errorMessage
                    };
                    _resultsData.Insert(0, resultData);
                }

                resultEntry.Add(imageContainer);

                if (texture != null)
                {
                    BgRemovalResultData resultData = new BgRemovalResultData
                    {
                        ProviderName = providerName_,
                        Timestamp = timestamp,
                        IsSuccess = true,
                        Texture = texture,
                        OriginalTexture = originalTexture_,
                        Width = texture.width,
                        Height = texture.height,
                        FileSize = imageBytes != null ? imageBytes.LongLength : 0
                    };
                    _resultsData.Insert(0, resultData);
                }
            }
            else if (!response_.IsSuccess)
            {
                TextField errorField = new TextField();
                errorField.value = response_.ErrorMessage;
                errorField.isReadOnly = true;
                errorField.multiline = true;
                errorField.AddToClassList("result-content");
                errorField.AddToClassList("selectable-content");
                resultEntry.Add(errorField);

                BgRemovalResultData resultData = new BgRemovalResultData
                {
                    ProviderName = providerName_,
                    Timestamp = timestamp,
                    IsSuccess = false,
                    ErrorMessage = response_.ErrorMessage
                };
                _resultsData.Insert(0, resultData);
            }

            _resultsContainer?.Insert(0, resultEntry);
        }

        private Texture2D CreateTextureFromBytes(byte[] imageBytes_)
        {
            if (imageBytes_ == null || imageBytes_.Length == 0)
                return null;

            try
            {
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes_))
                {
                    return texture;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIProvider] Failed to create texture: {ex.Message}");
            }

            return null;
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

        private VisualElement CreateImageSection(Texture2D texture_, string label_, bool withCheckerboard_)
        {
            VisualElement section = new VisualElement();
            section.AddToClassList("image-section");

            // Add label
            Label sectionLabel = new Label(label_);
            sectionLabel.AddToClassList("image-section-label");
            section.Add(sectionLabel);

            // Create image wrapper
            VisualElement imageWrapper = new VisualElement();
            imageWrapper.AddToClassList("result-image-wrapper");

            // Calculate display size maintaining aspect ratio
            float maxSize = 300f;
            float aspectRatio = (float)texture_.width / texture_.height;
            float displayWidth, displayHeight;

            if (texture_.width > texture_.height)
            {
                displayWidth = Mathf.Min(texture_.width, maxSize);
                displayHeight = displayWidth / aspectRatio;
            }
            else
            {
                displayHeight = Mathf.Min(texture_.height, maxSize);
                displayWidth = displayHeight * aspectRatio;
            }

            imageWrapper.style.width = displayWidth;
            imageWrapper.style.height = displayHeight;

            // Add checkerboard background if needed
            if (withCheckerboard_)
            {
                UnityEngine.UIElements.Image checkerboard = new UnityEngine.UIElements.Image();
                checkerboard.image = CreateCheckerboardTexture();
                checkerboard.scaleMode = ScaleMode.ScaleAndCrop;
                checkerboard.AddToClassList("checkerboard-background");
                checkerboard.style.position = Position.Absolute;
                checkerboard.style.width = Length.Percent(100);
                checkerboard.style.height = Length.Percent(100);
                imageWrapper.Add(checkerboard);
            }

            // Add actual image
            UnityEngine.UIElements.Image imageElement = new UnityEngine.UIElements.Image();
            imageElement.image = texture_;
            imageElement.AddToClassList("result-image");
            imageElement.scaleMode = ScaleMode.ScaleToFit;
            imageElement.style.position = Position.Absolute;
            imageElement.style.width = Length.Percent(100);
            imageElement.style.height = Length.Percent(100);

            imageWrapper.Add(imageElement);
            section.Add(imageWrapper);

            return section;
        }

        private void SaveImage(Texture2D texture_, string providerName_)
        {
            string defaultName = $"AIProvider_BgRemoval_{providerName_}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = EditorUtility.SaveFilePanel("Save Image", "", defaultName, "png");

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    byte[] pngData = texture_.EncodeToPNG();
                    File.WriteAllBytes(path, pngData);
                    SetStatus($"Image saved to: {path}");
                }
                catch (Exception ex)
                {
                    SetStatus($"Failed to save image: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            if (_clearButton != null)
            {
                _clearButton.clicked -= ClearResults;
            }

            if (_historyLoadButton != null)
            {
                _historyLoadButton.clicked -= OnHistoryLoadClicked;
            }
        }

        public void SaveHistoryIfNeeded()
        {
            if (_resultsData.Count == 0)
                return;

            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            BgRemovalHistorySnapshot snapshot = new BgRemovalHistorySnapshot
            {
                Id = EditorHistoryStorage.CreateEntryId(),
                CreatedAt = createdAt,
                DisplayName = createdAt,
                ItemCount = _resultsData.Count,
                Entries = new List<BgRemovalHistoryEntry>()
            };

            List<HistoryTextureData> textures = new List<HistoryTextureData>();
            int imageIndex = 0;

            foreach (BgRemovalResultData resultData in _resultsData)
            {
                BgRemovalHistoryEntry entry = new BgRemovalHistoryEntry
                {
                    ProviderName = resultData.ProviderName,
                    Timestamp = resultData.Timestamp,
                    IsSuccess = resultData.IsSuccess,
                    ErrorMessage = resultData.ErrorMessage,
                    Width = resultData.Width,
                    Height = resultData.Height,
                    FileSize = resultData.FileSize
                };

                if (resultData.IsSuccess && resultData.Texture != null)
                {
                    string fileName = $"result_{imageIndex}.png";
                    entry.ImageFileName = fileName;
                    HistoryTextureData textureData = new HistoryTextureData
                    {
                        FileName = fileName,
                        Texture = resultData.Texture
                    };
                    textures.Add(textureData);

                    // Save original image if available
                    if (resultData.OriginalTexture != null)
                    {
                        string originalFileName = $"original_{imageIndex}.png";
                        entry.OriginalImageFileName = originalFileName;
                        HistoryTextureData originalTextureData = new HistoryTextureData
                        {
                            FileName = originalFileName,
                            Texture = resultData.OriginalTexture
                        };
                        textures.Add(originalTextureData);
                    }

                    imageIndex++;
                }

                snapshot.Entries.Add(entry);
            }

            if (EditorHistoryStorage.SaveBgRemovalHistory(snapshot, textures))
            {
                RefreshHistoryList();
            }
        }

        public void LoadHistory(string entryId_)
        {
            BgRemovalHistorySnapshot snapshot = EditorHistoryStorage.LoadBgRemovalHistory(entryId_);
            if (snapshot == null || snapshot.Entries == null)
                return;

            _suppressHistorySave = true;
            ClearResults();

            for (int i = snapshot.Entries.Count - 1; i >= 0; i--)
            {
                BgRemovalHistoryEntry entry = snapshot.Entries[i];
                AddHistoryEntry(snapshot.Id, entry);
            }

            _suppressHistorySave = false;
        }

        private void AddHistoryEntry(string snapshotId_, BgRemovalHistoryEntry entry_)
        {
            VisualElement resultEntry = new VisualElement();
            resultEntry.AddToClassList("result-entry");

            VisualElement header = new VisualElement();
            header.AddToClassList("result-header");

            Label providerLabel = new Label(entry_.ProviderName);
            providerLabel.AddToClassList("result-provider-name");

            Label timestampLabel = new Label(entry_.Timestamp);
            timestampLabel.AddToClassList("result-timestamp");

            Label statusLabel = new Label(entry_.IsSuccess
                ? LocalizationManager.Get(LocalizationKeys.COMMON_SUCCESS)
                : LocalizationManager.Get(LocalizationKeys.COMMON_ERROR));
            statusLabel.AddToClassList("result-status");
            statusLabel.AddToClassList(entry_.IsSuccess ? "success" : "error");

            header.Add(providerLabel);
            header.Add(timestampLabel);
            header.Add(statusLabel);
            resultEntry.Add(header);

            if (entry_.IsSuccess && !string.IsNullOrEmpty(entry_.ImageFileName))
            {
                VisualElement imageContainer = new VisualElement();
                imageContainer.AddToClassList("result-image-container");

                Texture2D texture = EditorHistoryStorage.LoadTexture(HistoryFeatureType.BG_REMOVAL, snapshotId_, entry_.ImageFileName);
                if (texture != null)
                {
                    // Create before/after comparison container
                    VisualElement comparisonContainer = new VisualElement();
                    comparisonContainer.AddToClassList("before-after-container");

                    // Add original image (Before) if available
                    if (!string.IsNullOrEmpty(entry_.OriginalImageFileName))
                    {
                        Texture2D originalTexture = EditorHistoryStorage.LoadTexture(HistoryFeatureType.BG_REMOVAL, snapshotId_, entry_.OriginalImageFileName);
                        if (originalTexture != null)
                        {
                            VisualElement beforeSection = CreateImageSection(originalTexture, "Original", false);
                            comparisonContainer.Add(beforeSection);
                        }
                    }

                    // Add result image (After)
                    VisualElement afterSection = CreateImageSection(texture, "Result", true);
                    comparisonContainer.Add(afterSection);

                    imageContainer.Add(comparisonContainer);

                    Button saveButton = new Button(() => SaveImage(texture, entry_.ProviderName));
                    saveButton.text = LocalizationManager.Get(LocalizationKeys.BGREMOVAL_SAVE_BUTTON);
                    saveButton.AddToClassList("save-image-button");
                    imageContainer.Add(saveButton);

                    BgRemovalResultData resultData = new BgRemovalResultData
                    {
                        ProviderName = entry_.ProviderName,
                        Timestamp = entry_.Timestamp,
                        IsSuccess = true,
                        Texture = texture,
                        Width = texture.width,
                        Height = texture.height,
                        FileSize = entry_.FileSize
                    };
                    _resultsData.Insert(0, resultData);
                }
                else
                {
                    Label errorLabel = new Label(LocalizationManager.Get(LocalizationKeys.BGREMOVAL_TEXTURE_FAILED));
                    errorLabel.AddToClassList("error-label");
                    imageContainer.Add(errorLabel);
                }

                resultEntry.Add(imageContainer);
            }
            else if (!entry_.IsSuccess)
            {
                TextField errorField = new TextField();
                errorField.value = entry_.ErrorMessage;
                errorField.isReadOnly = true;
                errorField.multiline = true;
                errorField.AddToClassList("result-content");
                errorField.AddToClassList("selectable-content");
                resultEntry.Add(errorField);

                BgRemovalResultData resultData = new BgRemovalResultData
                {
                    ProviderName = entry_.ProviderName,
                    Timestamp = entry_.Timestamp,
                    IsSuccess = false,
                    ErrorMessage = entry_.ErrorMessage
                };
                _resultsData.Insert(0, resultData);
            }

            _resultsContainer?.Insert(0, resultEntry);
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
            _historyEntries = EditorHistoryStorage.ListEntries(HistoryFeatureType.BG_REMOVAL);
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
    }
}
