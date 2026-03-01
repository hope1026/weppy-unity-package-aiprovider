using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ImageResultsSection : VisualElement
    {
        private static readonly string SECTIONS_PATH = EditorPaths.VIEWS_PATH + "Image/Sections/";
        private static readonly string UXML_PATH = SECTIONS_PATH + "ImageResultsSection/ImageResultsSection.uxml";
        private static readonly string USS_PATH = SECTIONS_PATH + "ImageResultsSection/ImageResultsSection.uss";

        public event Action<string> OnStatusChanged;

        private VisualElement _resultsContainer;
        private Button _clearButton;
        private DropdownField _historyDropdown;
        private Button _historyLoadButton;
        private Label _historyLabel;
        private List<HistoryEntryInfo> _historyEntries = new List<HistoryEntryInfo>();
        private bool _suppressHistorySave;

        // Card-based state management
        private Dictionary<string, ImageCardData> _activeCards = new Dictionary<string, ImageCardData>();

        private enum CardState
        {
            PLACEHOLDER,
            LOADED,
            ERROR
        }

        private class ImageCardData
        {
            public string CardId { get; set; }
            public ImageEditorProviderType ProviderType { get; set; }
            public string ModelName { get; set; }
            public CardState State { get; set; }
            public VisualElement CardElement { get; set; }
            public Texture2D Texture { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public long FileSize { get; set; }
            public TextureFormat TextureFormat { get; set; }
            public string ErrorMessage { get; set; }
            public IVisualElementScheduledItem LoadingAnimation { get; set; }
        }

        public ImageResultsSection()
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
            _resultsContainer = this.Q<VisualElement>("image-results-container");
            _clearButton = this.Q<Button>("clear-image-button");
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

            // Dispose of all textures and stop animations
            foreach (ImageCardData cardData in _activeCards.Values)
            {
                if (cardData.Texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(cardData.Texture);
                }

                if (cardData.LoadingAnimation != null)
                {
                    cardData.LoadingAnimation.Pause();
                }
            }

            // Clear dictionary and UI
            _activeCards.Clear();
            _resultsContainer?.Clear();
        }

        public void AddResult(string providerName_, ImageResponse response_)
        {
            // Ensure this method runs on the main thread
            if (!MainThreadDispatcher.IsMainThread())
            {
                MainThreadDispatcher.Dispatch(() => AddResult(providerName_, response_));
                return;
            }

            // Parse provider type from provider name
            ImageEditorProviderType providerType = ParseProviderType(providerName_);
            string modelName = providerName_; // Default to provider name

            if (response_.IsSuccess && response_.Images.Count > 0)
            {
                // Try to find and update a placeholder for this provider
                string placeholderId = FindPlaceholderForProvider(providerType);

                // Process each image
                for (int i = 0; i < response_.Images.Count; i++)
                {
                    ImageResponseGeneratedImage image = response_.Images[i];
                    Texture2D texture = CreateTextureFromImage(image);

                    if (texture != null)
                    {
                        int width = texture.width;
                        int height = texture.height;
                        long fileSize = CalculateFileSizeFromBase64(image.Base64Data);

                        // For the first image, try to update placeholder
                        if (i == 0 && !string.IsNullOrEmpty(placeholderId))
                        {
                            UpdateCardWithImage(placeholderId, texture, width, height, fileSize);
                        }
                        else
                        {
                            // For other images or if no placeholder, create new card
                            CreateCardWithImage(providerType, modelName, texture, width, height, fileSize);
                        }
                    }
                    else
                    {
                        // Create error card for failed texture
                        string errorMsg = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_TEXTURE_FAILED);

                        if (i == 0 && !string.IsNullOrEmpty(placeholderId))
                        {
                            UpdateCardWithError(placeholderId, errorMsg);
                        }
                        else
                        {
                            CreateErrorCard(providerType, modelName, errorMsg);
                        }
                    }
                }
            }
            else if (!response_.IsSuccess)
            {
                Debug.LogError($"[AIProvider] AddResult: Error - {response_.ErrorMessage}");

                // Try to update placeholder with error
                string placeholderId = FindPlaceholderForProvider(providerType);

                if (!string.IsNullOrEmpty(placeholderId))
                {
                    UpdateCardWithError(placeholderId, response_.ErrorMessage);
                }
                else
                {
                    // Create new error card
                    CreateErrorCard(providerType, modelName, response_.ErrorMessage);
                }
            }
            else if (response_.IsSuccess && response_.Images.Count == 0)
            {
                Debug.LogWarning("[AIProvider] AddResult: Success but no images found");

                // Try to update placeholder with "no images" error
                string placeholderId = FindPlaceholderForProvider(providerType);
                string noImagesMsg = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_NO_IMAGES);

                if (!string.IsNullOrEmpty(placeholderId))
                {
                    UpdateCardWithError(placeholderId, noImagesMsg);
                }
                else
                {
                    CreateErrorCard(providerType, modelName, noImagesMsg);
                }
            }
        }

        private Texture2D CreateTextureFromImage(ImageResponseGeneratedImage image_)
        {
            return image_?.CreateTexture2D();
        }

        public void SaveHistoryIfNeeded()
        {
            List<ImageCardData> orderedCards = GetOrderedCardData();
            List<ImageCardData> saveableCards = new List<ImageCardData>();
            foreach (ImageCardData cardData in orderedCards)
            {
                if (cardData.State == CardState.LOADED || cardData.State == CardState.ERROR)
                {
                    saveableCards.Add(cardData);
                }
            }

            if (saveableCards.Count == 0)
                return;

            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ImageHistorySnapshot snapshot = new ImageHistorySnapshot
            {
                Id = EditorHistoryStorage.CreateEntryId(),
                CreatedAt = createdAt,
                DisplayName = createdAt,
                ItemCount = saveableCards.Count,
                Cards = new List<ImageHistoryCard>()
            };

            List<HistoryTextureData> textures = new List<HistoryTextureData>();
            int imageIndex = 0;
            foreach (ImageCardData cardData in saveableCards)
            {
                ImageHistoryCardState state = cardData.State == CardState.LOADED
                    ? ImageHistoryCardState.LOADED
                    : ImageHistoryCardState.ERROR;

                ImageHistoryCard card = new ImageHistoryCard
                {
                    State = state,
                    ProviderName = cardData.ProviderType.ToString(),
                    ModelName = cardData.ModelName,
                    Width = cardData.Width,
                    Height = cardData.Height,
                    FileSize = cardData.FileSize,
                    ErrorMessage = cardData.ErrorMessage
                };

                if (cardData.State == CardState.LOADED && cardData.Texture != null)
                {
                    string fileName = $"image_{imageIndex}.png";
                    card.ImageFileName = fileName;
                    HistoryTextureData textureData = new HistoryTextureData
                    {
                        FileName = fileName,
                        Texture = cardData.Texture
                    };
                    textures.Add(textureData);
                    imageIndex++;
                }

                snapshot.Cards.Add(card);
            }

            if (EditorHistoryStorage.SaveImageHistory(snapshot, textures))
            {
                RefreshHistoryList();
            }
        }

        public void LoadHistory(string entryId_)
        {
            ImageHistorySnapshot snapshot = EditorHistoryStorage.LoadImageHistory(entryId_);
            if (snapshot == null || snapshot.Cards == null)
                return;

            _suppressHistorySave = true;
            ClearResults();

            for (int i = snapshot.Cards.Count - 1; i >= 0; i--)
            {
                ImageHistoryCard card = snapshot.Cards[i];
                ImageEditorProviderType providerType = ParseProviderType(card.ProviderName);
                if (card.State == ImageHistoryCardState.LOADED)
                {
                    Texture2D texture = EditorHistoryStorage.LoadTexture(HistoryFeatureType.IMAGE, snapshot.Id, card.ImageFileName);
                    if (texture != null)
                    {
                        CreateCardWithImage(providerType, card.ModelName, texture, card.Width, card.Height, card.FileSize);
                    }
                    else
                    {
                        CreateErrorCard(providerType, card.ModelName, LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_TEXTURE_FAILED));
                    }
                }
                else if (card.State == ImageHistoryCardState.ERROR)
                {
                    CreateErrorCard(providerType, card.ModelName, card.ErrorMessage);
                }
            }

            _suppressHistorySave = false;
        }

        private void SaveImage(Texture2D texture_, string providerName_)
        {
            string defaultName = $"AIProvider_{providerName_}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
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

        private void DeleteImage(string cardId_)
        {
            if (!_activeCards.TryGetValue(cardId_, out ImageCardData cardData))
                return;

            // Dispose texture if exists
            if (cardData.Texture != null)
            {
                UnityEngine.Object.DestroyImmediate(cardData.Texture);
            }

            // Stop loading animation if exists
            if (cardData.LoadingAnimation != null)
            {
                cardData.LoadingAnimation.Pause();
            }

            // Remove from UI
            if (cardData.CardElement != null)
            {
                _resultsContainer?.Remove(cardData.CardElement);
            }

            // Remove from dictionary
            _activeCards.Remove(cardId_);

            SetStatus("Image deleted");
        }

        private ImageEditorProviderType ParseProviderType(string providerName_)
        {
            if (string.IsNullOrEmpty(providerName_))
                return ImageEditorProviderType.NONE;

            // Try direct enum parse first (e.g., "OPEN_AI", "GOOGLE_GEMINI", "CODEX_APP")
            if (System.Enum.TryParse(providerName_, true, out ImageEditorProviderType result))
                return result;

            // Handle decorated labels such as "CODEX_APP (Codex App Image)".
            int parenIndex = providerName_.IndexOf('(');
            if (parenIndex > 0)
            {
                string prefix = providerName_.Substring(0, parenIndex).Trim();
                if (System.Enum.TryParse(prefix, true, out result))
                    return result;
            }

            // Handle labels with trailing details separated by whitespace.
            int spaceIndex = providerName_.IndexOf(' ');
            if (spaceIndex > 0)
            {
                string firstToken = providerName_.Substring(0, spaceIndex).Trim();
                if (System.Enum.TryParse(firstToken, true, out result))
                    return result;
            }

            // Try to match common patterns
            string upperName = providerName_.ToUpper();

            if (upperName.Contains("OPENAI") || upperName.Contains("OPEN_AI") || upperName.Contains("GPT") || upperName.Contains("DALL"))
                return ImageEditorProviderType.OPEN_AI;

            if (upperName.Contains("CODEX"))
                return ImageEditorProviderType.CODEX_APP;

            if (upperName.Contains("GOOGLE") || upperName.Contains("GEMINI") || upperName.Contains("IMAGEN"))
            {
                if (upperName.Contains("IMAGEN"))
                    return ImageEditorProviderType.GOOGLE_IMAGEN;
                return ImageEditorProviderType.GOOGLE_GEMINI;
            }

            if (upperName.Contains("OPENROUTER") || upperName.Contains("OPEN_ROUTER"))
                return ImageEditorProviderType.OPEN_ROUTER;

            return ImageEditorProviderType.NONE;
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
            _historyEntries = EditorHistoryStorage.ListEntries(HistoryFeatureType.IMAGE);
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

        private List<ImageCardData> GetOrderedCardData()
        {
            List<ImageCardData> orderedCards = new List<ImageCardData>();
            if (_resultsContainer == null)
                return orderedCards;

            List<ImageCardData> allCards = new List<ImageCardData>(_activeCards.Values);
            allCards.Sort((left, right) =>
            {
                int leftIndex = _resultsContainer.IndexOf(left.CardElement);
                int rightIndex = _resultsContainer.IndexOf(right.CardElement);
                return leftIndex.CompareTo(rightIndex);
            });

            orderedCards.AddRange(allCards);
            return orderedCards;
        }

        private string FindPlaceholderForProvider(ImageEditorProviderType providerType_)
        {
            foreach (KeyValuePair<string, ImageCardData> kvp in _activeCards)
            {
                if (kvp.Value.State == CardState.PLACEHOLDER && kvp.Value.ProviderType == providerType_)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        private void CreateCardWithImage(ImageEditorProviderType providerType_, string modelName_, Texture2D texture_, int width_, int height_, long fileSize_)
        {
            string cardId = $"{providerType_}_{DateTime.Now.Ticks}_{_activeCards.Count}";

            ImageCardData cardData = new ImageCardData
            {
                CardId = cardId,
                ProviderType = providerType_,
                ModelName = modelName_,
                State = CardState.LOADED,
                Texture = texture_,
                Width = width_,
                Height = height_,
                FileSize = fileSize_,
                TextureFormat = texture_?.format ?? TextureFormat.RGBA32
            };

            VisualElement cardElement = CreateImageCard(cardData);
            cardData.CardElement = cardElement;

            _activeCards[cardId] = cardData;
            _resultsContainer?.Insert(0, cardElement);
        }

        private void CreateErrorCard(ImageEditorProviderType providerType_, string modelName_, string errorMessage_)
        {
            string cardId = $"{providerType_}_{DateTime.Now.Ticks}_{_activeCards.Count}";

            ImageCardData cardData = new ImageCardData
            {
                CardId = cardId,
                ProviderType = providerType_,
                ModelName = modelName_,
                State = CardState.ERROR,
                ErrorMessage = errorMessage_
            };

            VisualElement cardElement = CreateImageCard(cardData);
            cardData.CardElement = cardElement;

            _activeCards[cardId] = cardData;
            _resultsContainer?.Insert(0, cardElement);
        }

        public string CreatePlaceholder(ImageEditorProviderType providerType_, string modelName_)
        {
            // Generate unique card ID
            string cardId = $"{providerType_}_{DateTime.Now.Ticks}_{_activeCards.Count}";

            // Create card data
            ImageCardData cardData = new ImageCardData
            {
                CardId = cardId,
                ProviderType = providerType_,
                ModelName = modelName_,
                State = CardState.PLACEHOLDER
            };

            // Create card visual element
            VisualElement cardElement = CreateImageCard(cardData);
            cardData.CardElement = cardElement;

            // Start loading animation
            VisualElement spinner = cardElement.Q<VisualElement>(className: "image-result-loading-spinner");
            if (spinner != null)
            {
                cardData.LoadingAnimation = StartLoadingAnimation(spinner);
            }

            // Store card and add to container
            _activeCards[cardId] = cardData;
            _resultsContainer?.Insert(0, cardElement);

            return cardId;
        }

        public void ClearPlaceholders()
        {
            List<string> placeholderIds = new List<string>();

            foreach (KeyValuePair<string, ImageCardData> kvp in _activeCards)
            {
                if (kvp.Value.State == CardState.PLACEHOLDER)
                {
                    placeholderIds.Add(kvp.Key);
                }
            }

            foreach (string cardId in placeholderIds)
            {
                if (_activeCards.TryGetValue(cardId, out ImageCardData cardData))
                {
                    // Stop animation
                    if (cardData.LoadingAnimation != null)
                    {
                        cardData.LoadingAnimation.Pause();
                    }

                    // Remove from UI
                    if (cardData.CardElement != null)
                    {
                        _resultsContainer?.Remove(cardData.CardElement);
                    }

                    // Remove from dictionary
                    _activeCards.Remove(cardId);
                }
            }
        }

        public void UpdateCardWithImage(string cardId_, Texture2D texture_, int width_, int height_, long fileSize_)
        {
            if (!_activeCards.TryGetValue(cardId_, out ImageCardData cardData))
                return;

            // Stop loading animation
            if (cardData.LoadingAnimation != null)
            {
                cardData.LoadingAnimation.Pause();
                cardData.LoadingAnimation = null;
            }

            // Update card data
            cardData.State = CardState.LOADED;
            cardData.Texture = texture_;
            cardData.Width = width_;
            cardData.Height = height_;
            cardData.FileSize = fileSize_;
            cardData.TextureFormat = texture_?.format ?? TextureFormat.RGBA32;

            // Recreate card element
            if (cardData.CardElement != null && _resultsContainer != null)
            {
                int index = _resultsContainer.IndexOf(cardData.CardElement);
                _resultsContainer.Remove(cardData.CardElement);

                VisualElement newCardElement = CreateImageCard(cardData);
                cardData.CardElement = newCardElement;

                if (index >= 0 && index < _resultsContainer.childCount)
                {
                    _resultsContainer.Insert(index, newCardElement);
                }
                else
                {
                    _resultsContainer.Insert(0, newCardElement);
                }
            }
        }

        public void UpdateCardWithError(string cardId_, string errorMessage_)
        {
            if (!_activeCards.TryGetValue(cardId_, out ImageCardData cardData))
                return;

            // Stop loading animation
            if (cardData.LoadingAnimation != null)
            {
                cardData.LoadingAnimation.Pause();
                cardData.LoadingAnimation = null;
            }

            // Update card data
            cardData.State = CardState.ERROR;
            cardData.ErrorMessage = errorMessage_;

            // Recreate card element
            if (cardData.CardElement != null && _resultsContainer != null)
            {
                int index = _resultsContainer.IndexOf(cardData.CardElement);
                _resultsContainer.Remove(cardData.CardElement);

                VisualElement newCardElement = CreateImageCard(cardData);
                cardData.CardElement = newCardElement;

                if (index >= 0 && index < _resultsContainer.childCount)
                {
                    _resultsContainer.Insert(index, newCardElement);
                }
                else
                {
                    _resultsContainer.Insert(0, newCardElement);
                }
            }
        }

        private IVisualElementScheduledItem StartLoadingAnimation(VisualElement spinner_)
        {
            float rotation = 0f;

            return spinner_.schedule.Execute(() =>
            {
                rotation = (rotation + 10f) % 360f;
                spinner_.style.rotate = new Rotate(new Angle(rotation, AngleUnit.Degree));
            }).Every(16); // ~60fps
        }

        private VisualElement CreateImageCard(ImageCardData cardData_)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("image-result-card");

            if (cardData_.State == CardState.PLACEHOLDER)
            {
                card.AddToClassList("image-result-card--loading");
            }
            else if (cardData_.State == CardState.ERROR)
            {
                card.AddToClassList("image-result-card--error");
            }

            // Thumbnail container
            VisualElement thumbnailContainer = new VisualElement();
            thumbnailContainer.AddToClassList("image-result-thumbnail-container");

            if (cardData_.State == CardState.PLACEHOLDER)
            {
                // Loading state
                VisualElement loadingContainer = new VisualElement();
                loadingContainer.AddToClassList("image-result-loading-container");

                VisualElement spinner = new VisualElement();
                spinner.AddToClassList("image-result-loading-spinner");
                loadingContainer.Add(spinner);

                Label loadingText = new Label(LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_LOADING_CARD));
                loadingText.AddToClassList("image-result-loading-text");
                loadingContainer.Add(loadingText);

                thumbnailContainer.Add(loadingContainer);
            }
            else if (cardData_.State == CardState.ERROR)
            {
                // Error state
                Label errorLabel = new Label(cardData_.ErrorMessage ?? LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_ERROR_CARD));
                errorLabel.AddToClassList("image-result-error-text");
                thumbnailContainer.Add(errorLabel);
            }
            else if (cardData_.State == CardState.LOADED && cardData_.Texture != null)
            {
                // Loaded state - show image
                UnityEngine.UIElements.Image imageElement = new UnityEngine.UIElements.Image();
                imageElement.image = cardData_.Texture;
                imageElement.AddToClassList("image-result-thumbnail");
                imageElement.scaleMode = ScaleMode.ScaleToFit;
                thumbnailContainer.Add(imageElement);
            }

            card.Add(thumbnailContainer);

            // Metadata info (only show for loaded cards)
            if (cardData_.State == CardState.LOADED)
            {
                VisualElement infoContainer = new VisualElement();
                infoContainer.AddToClassList("image-result-info");

                // Model name
                Label modelLabel = new Label(cardData_.ModelName ?? "Unknown Model");
                modelLabel.AddToClassList("image-result-model-name");
                infoContainer.Add(modelLabel);

                // Dimensions
                if (cardData_.Width > 0 && cardData_.Height > 0)
                {
                    string aspectRatioText = CalculateAspectRatio(cardData_.Width, cardData_.Height);
                    Label dimensionsLabel = new Label($"{cardData_.Width}×{cardData_.Height} ({aspectRatioText})");
                    dimensionsLabel.AddToClassList("image-result-dimensions");
                    infoContainer.Add(dimensionsLabel);
                }

                VisualElement sizeAndFormatContainer = new VisualElement();
                sizeAndFormatContainer.AddToClassList("image-result-size-format-container");
                // File size
                if (cardData_.FileSize > 0)
                {
                    Label fileSizeLabel = new Label(FormatFileSize(cardData_.FileSize));
                    fileSizeLabel.AddToClassList("image-result-filesize");
                    sizeAndFormatContainer.Add(fileSizeLabel);
                }

                // Format info (File format and Pixel format)
                string formatInfo = $"{FormatTextureFormat(cardData_.TextureFormat)}";
                Label formatLabel = new Label(formatInfo);
                formatLabel.AddToClassList("image-result-format");
                sizeAndFormatContainer.Add(formatLabel);
                infoContainer.Add(sizeAndFormatContainer);

                card.Add(infoContainer);

                // Button container for Save and Delete buttons
                VisualElement buttonContainer = new VisualElement();
                buttonContainer.AddToClassList("image-result-button-container");

                // Delete button
                Button deleteButton = new Button(() => DeleteImage(cardData_.CardId));
                deleteButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_DELETE_BUTTON);
                deleteButton.AddToClassList("image-result-delete-button");
                buttonContainer.Add(deleteButton);
                
                // Save button (only if texture exists)
                if (cardData_.Texture != null)
                {
                    Button saveButton = new Button(() => SaveImage(cardData_.Texture, cardData_.ModelName));
                    saveButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_SAVE_BUTTON);
                    saveButton.AddToClassList("image-result-save-button");
                    buttonContainer.Add(saveButton);
                }

                card.Add(buttonContainer);
            }
            else if (cardData_.State == CardState.ERROR)
            {
                // Delete button for error cards
                Button deleteButton = new Button(() => DeleteImage(cardData_.CardId));
                deleteButton.text = LocalizationManager.Get(LocalizationKeys.IMAGE_GENERATION_DELETE_BUTTON);
                deleteButton.AddToClassList("image-result-delete-button");
                card.Add(deleteButton);
            }

            return card;
        }

        private long CalculateFileSizeFromBase64(string base64Data_)
        {
            if (string.IsNullOrEmpty(base64Data_))
                return 0;

            // Base64 encoding adds ~33% overhead
            // Actual file size ≈ base64Length * 0.75
            long base64Bytes = base64Data_.Length;
            return (long)(base64Bytes * 0.75);
        }

        private string FormatFileSize(long bytes_)
        {
            if (bytes_ < 1024)
                return $"{bytes_} B";
            else if (bytes_ < 1024 * 1024)
                return $"{bytes_ / 1024.0:F1} KB";
            else
                return $"{bytes_ / (1024.0 * 1024.0):F1} MB";
        }

        private string CalculateAspectRatio(int width_, int height_)
        {
            if (width_ <= 0 || height_ <= 0)
                return "N/A";

            int gcd = CalculateGcd(width_, height_);
            int ratioWidth = width_ / gcd;
            int ratioHeight = height_ / gcd;

            // If the simplified ratio has reasonable numbers (both <= 50), use integer ratio
            if (ratioWidth <= 50 && ratioHeight <= 50)
            {
                return $"{ratioWidth}:{ratioHeight}";
            }

            // Otherwise, fallback to decimal ratio
            float aspectRatio = (float)width_ / height_;
            if (Math.Abs(aspectRatio - Math.Round(aspectRatio)) < 0.05f)
            {
                return $"{(int)Math.Round(aspectRatio)}:1";
            }
            else
            {
                return $"{aspectRatio:F1}:1";
            }
        }

        private int CalculateGcd(int a_, int b_)
        {
            while (b_ != 0)
            {
                int temp = b_;
                b_ = a_ % b_;
                a_ = temp;
            }
            return a_;
        }

        private string FormatTextureFormat(TextureFormat format_)
        {
            switch (format_)
            {
                case TextureFormat.RGBA32:
                    return "RGBA32";
                case TextureFormat.ARGB32:
                    return "ARGB32";
                case TextureFormat.RGB24:
                    return "RGB24";
                case TextureFormat.RGBA64:
                    return "RGBA64";
                case TextureFormat.RGBAFloat:
                    return "RGBA Float";
                case TextureFormat.RGBAHalf:
                    return "RGBA Half";
                case TextureFormat.DXT1:
                    return "DXT1";
                case TextureFormat.DXT5:
                    return "DXT5";
                case TextureFormat.BC7:
                    return "BC7";
                default:
                    return format_.ToString();
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

            // Dispose of textures
            foreach (ImageCardData cardData in _activeCards.Values)
            {
                if (cardData.Texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(cardData.Texture);
                }

                // Stop loading animations
                if (cardData.LoadingAnimation != null)
                {
                    cardData.LoadingAnimation.Pause();
                }
            }

            _activeCards.Clear();
        }
    }
}
