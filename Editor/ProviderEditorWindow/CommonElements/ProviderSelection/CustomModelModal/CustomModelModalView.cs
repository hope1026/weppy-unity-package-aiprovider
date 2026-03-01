using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class CustomModelModalView<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        private const string MODAL_UXML_PATH =
            EditorPaths.EDITOR_WINDOW_PATH + "CommonElements/ProviderSelection/CustomModelModal/CustomModelModal.uxml";
        private const string MODAL_USS_PATH =
            EditorPaths.EDITOR_WINDOW_PATH + "CommonElements/ProviderSelection/CustomModelModal/CustomModelModal.uss";

        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;
        private readonly Action _onModelCreated;

        private VisualElement _modalOverlay;
        private Label _modalTitle;
        private Button _modalCloseButton;
        private Button _modalCancelButton;
        private Button _modalSaveButton;
        private Button _viewModelsButton;
        private Label _modalProviderName;
        private TextField _modalModelName;
        private TextField _modalModelId;
        private TextField _modalPricing;
        private TextField _modalTokens;
        private TextField _modalDescription;

        private Label _labelProvider;
        private Label _labelLocked;
        private Label _labelModelName;
        private Label _labelModelId;
        private Label _labelPricing;
        private Label _labelTokens;
        private Label _labelDescription;

        private TProviderType _currentProviderType;
        private bool _hasProvider;

        public CustomModelModalView(
            ProviderModelSelectionConfig<TProviderType, TModelInfo> config_,
            Action onModelCreated_)
        {
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
            _onModelCreated = onModelCreated_;
        }

        public VisualElement CreateModalElement()
        {
            VisualTreeAsset modalTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MODAL_UXML_PATH);
            if (modalTree == null)
            {
                Debug.LogError($"Failed to load modal UXML at: {MODAL_UXML_PATH}");
                return new VisualElement();
            }

            TemplateContainer modalRoot = modalTree.CloneTree();

            StyleSheet modalStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(MODAL_USS_PATH);
            if (modalStyles != null)
            {
                modalRoot.styleSheets.Add(modalStyles);
            }

            modalRoot.style.position = Position.Absolute;
            modalRoot.style.left = 0;
            modalRoot.style.top = 0;
            modalRoot.style.right = 0;
            modalRoot.style.bottom = 0;
            modalRoot.pickingMode = PickingMode.Ignore;

            InitializeElements(modalRoot);
            SetupLocalization();
            SetupPlaceholders();
            RegisterCallbacks();

            return modalRoot;
        }

        private void InitializeElements(VisualElement root_)
        {
            _modalOverlay = root_.Q<VisualElement>("cmm-modal-overlay");
            _modalTitle = root_.Q<Label>("cmm-modal-title");
            _modalCloseButton = root_.Q<Button>("cmm-modal-close");
            _modalCancelButton = root_.Q<Button>("cmm-modal-cancel");
            _modalSaveButton = root_.Q<Button>("cmm-modal-save");
            _viewModelsButton = root_.Q<Button>("cmm-provider-models-btn");
            _modalProviderName = root_.Q<Label>("cmm-modal-provider-name");
            _modalModelName = root_.Q<TextField>("cmm-modal-model-name");
            _modalModelId = root_.Q<TextField>("cmm-modal-model-id");
            _modalPricing = root_.Q<TextField>("cmm-modal-pricing");
            _modalTokens = root_.Q<TextField>("cmm-modal-tokens");
            _modalDescription = root_.Q<TextField>("cmm-modal-description");

            _labelProvider = root_.Q<Label>("cmm-label-provider");
            _labelLocked = root_.Q<Label>("cmm-label-locked");
            _labelModelName = root_.Q<Label>("cmm-label-model-name");
            _labelModelId = root_.Q<Label>("cmm-label-model-id");
            _labelPricing = root_.Q<Label>("cmm-label-pricing");
            _labelTokens = root_.Q<Label>("cmm-label-tokens");
            _labelDescription = root_.Q<Label>("cmm-label-description");
        }

        private void SetupLocalization()
        {
            if (_modalTitle != null)
                _modalTitle.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_TITLE);

            if (_modalCancelButton != null)
                _modalCancelButton.text = LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL);

            if (_modalSaveButton != null)
                _modalSaveButton.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_SAVE_BUTTON);

            if (_viewModelsButton != null)
                _viewModelsButton.text = LocalizationManager.Get(LocalizationKeys.PROVIDER_OFFICIAL_DOCS);

            if (_labelProvider != null)
                _labelProvider.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_PROVIDER).ToUpper();

            if (_labelLocked != null)
                _labelLocked.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_LOCKED);

            if (_labelModelName != null)
                _labelModelName.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_NAME).ToUpper();

            if (_labelModelId != null)
                _labelModelId.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_ID).ToUpper();

            if (_labelPricing != null)
                _labelPricing.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_PRICING).ToUpper();

            if (_labelTokens != null)
                _labelTokens.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_TOKENS).ToUpper();

            if (_labelDescription != null)
                _labelDescription.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_DESCRIPTION).ToUpper();
        }

        private void SetupPlaceholders()
        {
            SetPlaceholder(_modalModelName, LocalizationKeys.CUSTOM_MODEL_NAME_PLACEHOLDER);
            SetPlaceholder(_modalModelId, LocalizationKeys.CUSTOM_MODEL_ID_PLACEHOLDER);
            SetPlaceholder(_modalPricing, LocalizationKeys.CUSTOM_MODEL_PRICING_PLACEHOLDER);
            SetPlaceholder(_modalTokens, LocalizationKeys.CUSTOM_MODEL_TOKENS_PLACEHOLDER);
            SetPlaceholder(_modalDescription, LocalizationKeys.CUSTOM_MODEL_DESCRIPTION_PLACEHOLDER);
        }

        private void SetPlaceholder(TextField textField_, string localizationKey_)
        {
            if (textField_ == null)
                return;

            TextField internalTextField = textField_.Q<TextField>();
            if (internalTextField != null)
            {
                internalTextField.textEdition.placeholder = LocalizationManager.Get(localizationKey_);
            }
            else
            {
                textField_.textEdition.placeholder = LocalizationManager.Get(localizationKey_);
            }
        }

        private void RegisterCallbacks()
        {
            if (_modalCloseButton != null)
            {
                _modalCloseButton.clicked += Close;
            }

            if (_modalCancelButton != null)
            {
                _modalCancelButton.clicked += Close;
            }

            if (_modalSaveButton != null)
            {
                _modalSaveButton.clicked += SaveCustomModel;
            }

            if (_viewModelsButton != null)
            {
                _viewModelsButton.clicked += OpenModelsUrl;
            }

            if (_modalOverlay != null)
            {
                _modalOverlay.RegisterCallback<ClickEvent>(evt_ =>
                {
                    if (evt_.target == _modalOverlay)
                    {
                        Close();
                    }
                });
            }
        }

        private void OpenModelsUrl()
        {
            if (!_hasProvider)
                return;

            string url = _config.GetModelsUrl != null
                ? _config.GetModelsUrl(_currentProviderType)
                : string.Empty;

            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        public void Open(TProviderType providerType_)
        {
            if (_modalOverlay == null)
                return;

            _currentProviderType = providerType_;
            _hasProvider = true;

            if (_modalProviderName != null)
            {
                _modalProviderName.text = providerType_.ToString();
            }

            ClearFields();
            _modalOverlay.style.display = DisplayStyle.Flex;
        }

        public void Close()
        {
            if (_modalOverlay == null)
                return;

            _modalOverlay.style.display = DisplayStyle.None;
            ClearFields();
        }

        private void ClearFields()
        {
            if (_modalModelName != null) _modalModelName.value = string.Empty;
            if (_modalModelId != null) _modalModelId.value = string.Empty;
            if (_modalPricing != null) _modalPricing.value = string.Empty;
            if (_modalTokens != null) _modalTokens.value = string.Empty;
            if (_modalDescription != null) _modalDescription.value = string.Empty;
        }

        private void SaveCustomModel()
        {
            if (!_hasProvider || _config.AddCustomModel == null || _config.CreateCustomModelInfo == null)
                return;

            string modelName = _modalModelName?.value?.Trim();
            string modelId = _modalModelId?.value?.Trim();
            string pricing = _modalPricing?.value?.Trim();
            string tokens = _modalTokens?.value?.Trim();
            string description = _modalDescription?.value?.Trim();

            if (string.IsNullOrEmpty(modelId))
            {
                Debug.LogWarning(LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_ID_REQUIRED));
                return;
            }

            if (string.IsNullOrEmpty(modelName))
            {
                modelName = modelId;
            }

            string currentLanguageCode = LocalizationManager.CurrentLanguageCode;

            try
            {
                TModelInfo newModel = _config.CreateCustomModelInfo(_currentProviderType);
                if (newModel == null)
                {
                    Debug.LogError("Failed to create custom model info.");
                    return;
                }

                if (newModel is ImageModelInfo imageModel)
                {
                    imageModel.Id = modelId;
                    imageModel.DisplayName = modelName;
                    imageModel.IsCustom = true;

                    if (double.TryParse(pricing, out double priceValue))
                    {
                        imageModel.PricePerImage = priceValue;
                    }
                    else if (!string.IsNullOrEmpty(pricing))
                    {
                        imageModel.PricePerImageText = pricing;
                        imageModel.NormalizePriceFromText();
                    }

                    if (!string.IsNullOrEmpty(description))
                        imageModel.SetDescription(currentLanguageCode, description);
                }
                else if (newModel is BgRemovalModelInfo bgRemovalModel)
                {
                    bgRemovalModel.Id = modelId;
                    bgRemovalModel.DisplayName = modelName;
                    bgRemovalModel.IsCustom = true;

                    if (double.TryParse(pricing, out double priceValue))
                    {
                        bgRemovalModel.PricePerImage = priceValue;
                    }
                    else if (!string.IsNullOrEmpty(pricing))
                    {
                        bgRemovalModel.PricePerImageText = pricing;
                        bgRemovalModel.NormalizePriceFromText();
                    }

                    if (!string.IsNullOrEmpty(description))
                        bgRemovalModel.SetDescription(currentLanguageCode, description);
                }
                else if (newModel is ChatModelInfo chatModel)
                {
                    chatModel.Id = modelId;
                    chatModel.DisplayName = modelName;
                    chatModel.IsCustom = true;

                    if (!string.IsNullOrEmpty(description))
                        chatModel.SetDescription(currentLanguageCode, description);

                    if (int.TryParse(tokens, out int contextWindow))
                    {
                        chatModel.ContextWindowSize = contextWindow;
                    }
                }

                _config.AddCustomModel(_currentProviderType, newModel);

                Close();
                _onModelCreated?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create custom model: {e.Message}");
            }
        }

        public void Cleanup()
        {
            if (_modalCloseButton != null)
            {
                _modalCloseButton.clicked -= Close;
            }

            if (_modalCancelButton != null)
            {
                _modalCancelButton.clicked -= Close;
            }

            if (_modalSaveButton != null)
            {
                _modalSaveButton.clicked -= SaveCustomModel;
            }

            if (_viewModelsButton != null)
            {
                _viewModelsButton.clicked -= OpenModelsUrl;
            }

            _modalOverlay = null;
            _modalTitle = null;
            _modalCloseButton = null;
            _modalCancelButton = null;
            _modalSaveButton = null;
            _viewModelsButton = null;
            _modalProviderName = null;
            _modalModelName = null;
            _modalModelId = null;
            _modalPricing = null;
            _modalTokens = null;
            _modalDescription = null;
            _labelProvider = null;
            _labelLocked = null;
            _labelModelName = null;
            _labelModelId = null;
            _labelPricing = null;
            _labelTokens = null;
            _labelDescription = null;
        }
    }
}
