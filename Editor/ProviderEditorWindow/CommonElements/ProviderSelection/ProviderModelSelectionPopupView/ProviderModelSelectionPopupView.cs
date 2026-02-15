using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Chat.Editor
{
    public class ProviderModelSelectionPopupView<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        private enum ModelListWarningType
        {
            NONE,
            API_KEY,
            CLI_PATH
        }

        private enum ModelSortOrder
        {
            PRIORITY_DESCENDING,
            PRICE_ASCENDING,
            PRICE_DESCENDING,
            CONTEXT_DESCENDING
        }

        private struct ModelEntry
        {
            public TProviderType providerType;
            public TModelInfo modelInfo;
            public string modelId;
            public string displayName;
            public string description;
            public bool isSelected;
            public bool isProviderEnabled;
            public bool isCustom;
            public int priority;
            public string key;
        }

        private const string POPUP_UXML_PATH =
            EditorPaths.EDITOR_WINDOW_PATH + "CommonElements/ProviderSelection/ProviderModelSelectionPopupView/ProviderModelSelectionPopup.uxml";

        private const string POPUP_USS_PATH =
            EditorPaths.EDITOR_WINDOW_PATH + "CommonElements/ProviderSelection/ProviderModelSelectionPopupView/ProviderModelSelectionPopup.uss";

        private readonly ProviderModelSelectionElement<TProviderType, TModelInfo> _owner;
        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;

        private string _providerSearchText = string.Empty;
        private string _modelSearchText = string.Empty;
        private bool _hasActiveProvider;
        private TProviderType _activeProviderType;
        private ModelSortOrder _currentSortOrder = ModelSortOrder.PRIORITY_DESCENDING;

        private readonly Dictionary<TProviderType, VisualElement> _providerRowByType =
            new Dictionary<TProviderType, VisualElement>();

        private readonly Dictionary<TProviderType, Label> _providerSelectedCountLabels =
            new Dictionary<TProviderType, Label>();

        private ToolbarSearchField _providerSearchField;
        private ToolbarSearchField _modelSearchField;
        private ToolbarMenu _sortDropdown;
        private VisualElement _providerListContainer;
        private VisualElement _providerAllContainer;
        private VisualElement _modelListContainer;
        private VisualElement _modelSelectedTabsContainer;
        private VisualElement _headerSelectedTabsContainer;
        private Label _modelPanelTitle;
        private Label _popupTitleLabel;
        private Button _closeButton;
        private VisualElement _allProvidersRow;
        private bool _isAllProvidersActive;
        private DragDropReorderController<string> _modelDragDropController;
        private readonly Dictionary<string, bool> _modelDraggableByKey = new Dictionary<string, bool>();

        private readonly Dictionary<string, (TProviderType providerType, string modelId)> _modelKeyMap =
            new Dictionary<string, (TProviderType providerType, string modelId)>();

        // Custom Model Modal
        private CustomModelModalView<TProviderType, TModelInfo> _customModelModalView;
        private Button _addModelButton;
        private Button _deleteAllCustomButton;
        private Button _officialDocButton;
        private Label _warningText;

        // No API Key Warning
        private VisualElement _noApiKeyWarning;
        private Label _noApiKeyText;
        private Button _goToSettingsButton;
        private ModelListWarningType _modelListWarningType = ModelListWarningType.NONE;

        // API Key Section
        private VisualElement _apiKeySection;
        private Label _apiKeyTitle;
        private Label _apiKeyStatus;
        private Label _apiKeyLabel;
        private VisualElement _apiKeyOptionalRow;
        private Toggle _apiKeyOptionalToggle;
        private VisualElement _apiKeyLabelRow;
        private VisualElement _apiKeyViewMode;
        private VisualElement _apiKeyEditMode;
        private TextField _apiKeyFieldReadonly;
        private TextField _apiKeyFieldEditable;
        private Button _editApiKeyButton;
        private Button _deleteApiKeyButton;
        private Button _saveApiKeyButton;
        private Button _cancelEditButton;
        private Button _getApiKeyButton;

        private VisualElement _cliPathSection;
        private Label _cliPathTitle;
        private TextField _cliPathField;
        private Button _cliPathAutoDetectButton;
        private Button _cliPathBrowseButton;
        private Button _cliPathGuideButton;
        private VisualElement _noCliPathWarning;
        private Label _noCliPathText;
        private Button _goToCliPathButton;

        private VisualElement _nodePathSection;
        private Label _nodePathTitle;
        private TextField _nodePathField;
        private Button _nodePathAutoDetectButton;
        private Button _nodePathBrowseButton;

        private bool _isEditingApiKey;
        private string _currentApiKey = string.Empty;
        private EditorDataStorage _storage;

        public ProviderModelSelectionPopupView(
            ProviderModelSelectionElement<TProviderType, TModelInfo> owner_,
            ProviderModelSelectionConfig<TProviderType, TModelInfo> config_,
            EditorDataStorage storage_)
        {
            _owner = owner_ ?? throw new ArgumentNullException(nameof(owner_));
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
            _storage = storage_ ?? throw new ArgumentNullException(nameof(storage_));
        }

        public void Toggle()
        {
            if (_owner.ModelSelectionPopup != null)
            {
                Close();
                return;
            }

            Show();
        }

        public void Show()
        {
            if (_owner.ModelSelectionPopup != null)
            {
                return;
            }

            VisualElement root = _owner.GetRootVisualContainer();
            if (root == null)
            {
                return;
            }

            _owner.PopupOverlay = new VisualElement();
            _owner.PopupOverlay.name = "provider-model-selection-overlay";
            _owner.PopupOverlay.AddToClassList("pms-overlay");
            _owner.PopupOverlay.pickingMode = PickingMode.Position;
            _owner.PopupOverlay.RegisterCallback<ClickEvent>(evt_ =>
            {
                if (evt_.target == _owner.PopupOverlay)
                {
                    Close();
                }
            });

            if (!string.IsNullOrEmpty(_config.SectionUssPath))
            {
                StyleSheet sectionStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(_config.SectionUssPath);
                if (sectionStyles != null)
                {
                    _owner.PopupOverlay.styleSheets.Add(sectionStyles);
                }
            }

            StyleSheet popupStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(POPUP_USS_PATH);
            if (popupStyles != null)
            {
                _owner.PopupOverlay.styleSheets.Add(popupStyles);
            }

            _owner.ModelSelectionPopup = CreateModelSelectionPopup();
            _owner.ModelSelectionPopup.AddToClassList("pms-popup");

            if (_owner.ModelSelectionButton != null)
            {
                Rect buttonRect = _owner.ModelSelectionButton.worldBound;
                Rect rootRect = root.worldBound;

                float relativeLeft = buttonRect.x - rootRect.x;
                float relativeTop = buttonRect.yMax - rootRect.y + 6;

                _owner.ModelSelectionPopup.style.left = relativeLeft;
                _owner.ModelSelectionPopup.style.top = relativeTop;
            }
            else
            {
                _owner.ModelSelectionPopup.style.left = 20;
                _owner.ModelSelectionPopup.style.top = 80;
            }

            _owner.PopupOverlay.Add(_owner.ModelSelectionPopup);
            root.Add(_owner.PopupOverlay);
            _owner.PopupOverlay.BringToFront();

            _owner.PopupOverlay.MarkDirtyRepaint();
            _owner.ModelSelectionPopup.MarkDirtyRepaint();
        }

        public void Close()
        {
            if (_owner.PopupOverlay != null)
            {
                _owner.PopupOverlay.RemoveFromHierarchy();
                _owner.PopupOverlay = null;
            }

            _owner.ModelSelectionPopup = null;
            _owner.PopupProviderList = null;
            _owner.PopupDragDropController = null;

            _providerSearchField = null;
            _modelSearchField = null;
            _providerListContainer = null;
            _providerAllContainer = null;
            _modelListContainer = null;
            _modelDragDropController = null;
            _modelSelectedTabsContainer = null;
            _headerSelectedTabsContainer = null;
            _modelPanelTitle = null;
            _popupTitleLabel = null;
            _closeButton = null;
            _providerSearchText = string.Empty;
            _modelSearchText = string.Empty;
            _providerRowByType.Clear();
            _providerSelectedCountLabels.Clear();
            _allProvidersRow = null;
            _isAllProvidersActive = false;

            _customModelModalView?.Cleanup();
            _customModelModalView = null;
            _addModelButton = null;
            _deleteAllCustomButton = null;
            _officialDocButton = null;
            _warningText = null;

            _noApiKeyWarning = null;
            _noApiKeyText = null;
            _goToSettingsButton = null;

            // API Key Section cleanup
            _apiKeySection = null;
            _apiKeyTitle = null;
            _apiKeyStatus = null;
            _apiKeyLabel = null;
            _apiKeyOptionalRow = null;
            _apiKeyOptionalToggle = null;
            _apiKeyLabelRow = null;
            _apiKeyViewMode = null;
            _apiKeyEditMode = null;
            _apiKeyFieldReadonly = null;
            _apiKeyFieldEditable = null;
            _editApiKeyButton = null;
            _deleteApiKeyButton = null;
            _saveApiKeyButton = null;
            _cancelEditButton = null;
            _getApiKeyButton = null;
            _isEditingApiKey = false;
            _currentApiKey = string.Empty;
        }

        public void RefreshLanguage(string languageCode_)
        {
            if (_owner.ModelSelectionPopup == null)
            {
                return;
            }

            SetupActionButtons();
            SetupWarningBanner();
            SetupNoApiKeyWarning();
            SetupApiKeySection();
            RefreshActiveProviderModels();
        }

        public void SetActiveProvider(TProviderType providerType_, bool rebuildModels_)
        {
            // If currently editing API key, cancel edit mode when switching providers
            if (_isEditingApiKey && _hasActiveProvider && !EqualityComparer<TProviderType>.Default.Equals(providerType_, _activeProviderType))
            {
                _isEditingApiKey = false;
            }

            _activeProviderType = providerType_;
            _hasActiveProvider = true;
            _isAllProvidersActive = false;

            bool hasKey = HasApiKeyAvailable(providerType_);

            // Load and update API key section
            UpdateApiKeySection(providerType_, hasKey);
            UpdateCliPathSection(providerType_);
            UpdateNodePathSection(providerType_);

            UpdateModelListWarning(providerType_);

            UpdateActiveProviderVisuals();

            if (rebuildModels_)
            {
                RefreshActiveProviderModels();
            }
        }

        private VisualElement CreateModelSelectionPopup()
        {
            VisualElement popup = new VisualElement();
            popup.name = "provider-model-selection-popup-container";

            VisualTreeAsset popupTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(POPUP_UXML_PATH);
            if (popupTree != null)
            {
                popupTree.CloneTree(popup);
            }

            InitializePopupElements(popup);
            RegisterPopupCallbacks();

            if (_popupTitleLabel != null)
            {
                _popupTitleLabel.text = "Models";
            }

            List<TProviderType> providerOrder = _owner.LoadProviderOrder();
            List<TProviderType> sortedOrder = _owner.SortProvidersByEnabledState(providerOrder);
            _owner.PopupDragDropController = new DragDropReorderController<TProviderType>(
                _providerListContainer,
                itemType_ => true,
                OnPopupProviderOrderChanged,
                null);
            _owner.PopupDragDropController.Initialize(sortedOrder);

            BuildProviderList();

            if (_isAllProvidersActive)
            {
                HideModelListWarning();
                UpdateActiveProviderVisuals();
                RefreshActiveProviderModels();
            }
            else
            {
                bool hasStoredActiveProvider = _hasActiveProvider && sortedOrder.Contains(_activeProviderType);
                if (!hasStoredActiveProvider)
                {
                    if (sortedOrder.Count > 0)
                    {
                        SetActiveProvider(sortedOrder[0], true);
                    }
                }
                else
                {
                    SetActiveProvider(_activeProviderType, true);
                }
            }

            BuildHeaderSelectedChips();

            return popup;
        }

        private void InitializePopupElements(VisualElement popup_)
        {
            _providerSearchField = popup_.Q<ToolbarSearchField>("pms-provider-search");
            _modelSearchField = popup_.Q<ToolbarSearchField>("pms-model-search");
            _sortDropdown = popup_.Q<ToolbarMenu>("pms-model-sort");
            _providerListContainer = popup_.Q<VisualElement>("pms-provider-list");
            _providerAllContainer = popup_.Q<VisualElement>("pms-provider-all");
            _modelListContainer = popup_.Q<VisualElement>("pms-model-list");
            _modelSelectedTabsContainer = popup_.Q<VisualElement>("pms-model-selected-tabs");
            _headerSelectedTabsContainer = popup_.Q<VisualElement>("pms-header-selected-tabs");
            _modelPanelTitle = popup_.Q<Label>("pms-model-title");
            _popupTitleLabel = popup_.Q<Label>("pms-title");
            _closeButton = popup_.Q<Button>("pms-close");
            _owner.PopupProviderList = _providerListContainer;

            // Initialize Custom Model Modal
            _customModelModalView = new CustomModelModalView<TProviderType, TModelInfo>(
                _config,
                RefreshActiveProviderModels);

            VisualElement modalElement = _customModelModalView.CreateModalElement();
            popup_.Add(modalElement);

            _addModelButton = popup_.Q<Button>("pms-add-model-btn");
            _deleteAllCustomButton = popup_.Q<Button>("pms-delete-custom-btn");
            _officialDocButton = popup_.Q<Button>("pms-official-doc-btn");
            _warningText = popup_.Q<Label>("pms-warning-text");

            _noApiKeyWarning = popup_.Q<VisualElement>("pms-no-api-key-warning");
            _noApiKeyText = popup_.Q<Label>("pms-no-api-key-text");
            _goToSettingsButton = popup_.Q<Button>("pms-go-to-settings-btn");

            // Initialize API Key Section
            _apiKeySection = popup_.Q<VisualElement>("pms-api-key-section");
            _apiKeyTitle = popup_.Q<Label>("pms-api-key-title");
            _apiKeyStatus = popup_.Q<Label>("pms-api-key-status");
            _apiKeyLabel = popup_.Q<Label>("pms-api-key-label");
            _apiKeyOptionalRow = popup_.Q<VisualElement>("pms-api-key-optional-row");
            _apiKeyOptionalToggle = popup_.Q<Toggle>("pms-api-key-optional-toggle");
            _apiKeyLabelRow = popup_.Q<VisualElement>("pms-api-key-label-row");
            _apiKeyViewMode = popup_.Q<VisualElement>("pms-api-key-view-mode");
            _apiKeyEditMode = popup_.Q<VisualElement>("pms-api-key-edit-mode");
            _apiKeyFieldReadonly = popup_.Q<TextField>("pms-api-key-field-readonly");
            _apiKeyFieldEditable = popup_.Q<TextField>("pms-api-key-field-editable");
            _editApiKeyButton = popup_.Q<Button>("pms-edit-api-key-btn");
            _deleteApiKeyButton = popup_.Q<Button>("pms-delete-api-key-btn");
            _saveApiKeyButton = popup_.Q<Button>("pms-save-api-key-btn");
            _cancelEditButton = popup_.Q<Button>("pms-cancel-edit-btn");
            _getApiKeyButton = popup_.Q<Button>("pms-get-api-key-btn");

            _cliPathSection = popup_.Q<VisualElement>("pms-cli-path-section");
            _cliPathTitle = popup_.Q<Label>("pms-cli-path-title");
            _cliPathField = popup_.Q<TextField>("pms-cli-path-field");
            _cliPathAutoDetectButton = popup_.Q<Button>("pms-cli-path-auto-btn");
            _cliPathBrowseButton = popup_.Q<Button>("pms-cli-path-browse-btn");
            _cliPathGuideButton = popup_.Q<Button>("pms-cli-path-guide-btn");
            _noCliPathWarning = popup_.Q<VisualElement>("pms-no-cli-path-warning");
            _noCliPathText = popup_.Q<Label>("pms-no-cli-path-text");
            _goToCliPathButton = popup_.Q<Button>("pms-go-to-cli-path-btn");

            _nodePathSection = popup_.Q<VisualElement>("pms-node-path-section");
            _nodePathTitle = popup_.Q<Label>("pms-node-path-title");
            _nodePathField = popup_.Q<TextField>("pms-node-path-field");
            _nodePathAutoDetectButton = popup_.Q<Button>("pms-node-path-auto-btn");
            _nodePathBrowseButton = popup_.Q<Button>("pms-node-path-browse-btn");

            SetupPlaceholderText();
            SetupSortDropdown();
            SetupActionButtons();
            SetupWarningBanner();
            SetupNoApiKeyWarning();
            SetupApiKeySection();
            SetupCliPathSection();
            SetupNodePathSection();
        }

        private void SetupPlaceholderText()
        {
            if (_providerSearchField != null)
            {
                _providerSearchField.value = string.Empty;
                TextField providerTextField = _providerSearchField.Q<TextField>();
                if (providerTextField != null)
                {
                    providerTextField.textEdition.placeholder = LocalizationManager.Get(LocalizationKeys.COMMON_SEARCH_PLACEHOLDER_PROVIDERS);
                }
            }

            if (_modelSearchField != null)
            {
                _modelSearchField.value = string.Empty;
                TextField modelTextField = _modelSearchField.Q<TextField>();
                if (modelTextField != null)
                {
                    modelTextField.textEdition.placeholder = LocalizationManager.Get(LocalizationKeys.COMMON_SEARCH_PLACEHOLDER_MODELS);
                }
            }
        }

        private void SetupSortDropdown()
        {
            if (_sortDropdown == null)
                return;

            _sortDropdown.text = LocalizationManager.Get(LocalizationKeys.COMMON_SORT_PRIORITY_DESC);

            _sortDropdown.menu.AppendAction(
                LocalizationManager.Get(LocalizationKeys.COMMON_SORT_PRIORITY_DESC),
                _ => ChangeSortOrder(ModelSortOrder.PRIORITY_DESCENDING),
                _ => _currentSortOrder == ModelSortOrder.PRIORITY_DESCENDING ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            _sortDropdown.menu.AppendAction(
                LocalizationManager.Get(LocalizationKeys.COMMON_SORT_PRICE_ASC),
                _ => ChangeSortOrder(ModelSortOrder.PRICE_ASCENDING),
                _ => _currentSortOrder == ModelSortOrder.PRICE_ASCENDING ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            _sortDropdown.menu.AppendAction(
                LocalizationManager.Get(LocalizationKeys.COMMON_SORT_PRICE_DESC),
                _ => ChangeSortOrder(ModelSortOrder.PRICE_DESCENDING),
                _ => _currentSortOrder == ModelSortOrder.PRICE_DESCENDING ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            _sortDropdown.menu.AppendAction(
                LocalizationManager.Get(LocalizationKeys.COMMON_SORT_CONTEXT_DESC),
                _ => ChangeSortOrder(ModelSortOrder.CONTEXT_DESCENDING),
                _ => _currentSortOrder == ModelSortOrder.CONTEXT_DESCENDING ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        private void ChangeSortOrder(ModelSortOrder sortOrder_)
        {
            _currentSortOrder = sortOrder_;

            string sortText = _currentSortOrder switch
            {
                ModelSortOrder.PRICE_ASCENDING => LocalizationManager.Get(LocalizationKeys.COMMON_SORT_PRICE_ASC),
                ModelSortOrder.PRICE_DESCENDING => LocalizationManager.Get(LocalizationKeys.COMMON_SORT_PRICE_DESC),
                ModelSortOrder.CONTEXT_DESCENDING => LocalizationManager.Get(LocalizationKeys.COMMON_SORT_CONTEXT_DESC),
                _ => LocalizationManager.Get(LocalizationKeys.COMMON_SORT_PRIORITY_DESC)
            };

            if (_sortDropdown != null)
            {
                _sortDropdown.text = sortText;
            }

            RefreshActiveProviderModels();
        }

        private void RegisterPopupCallbacks()
        {
            if (_providerSearchField != null)
            {
                _providerSearchField.RegisterValueChangedCallback(evt_ =>
                {
                    _providerSearchText = evt_.newValue ?? string.Empty;
                    BuildProviderList();
                });
            }

            if (_modelSearchField != null)
            {
                _modelSearchField.RegisterValueChangedCallback(evt_ =>
                {
                    _modelSearchText = evt_.newValue ?? string.Empty;
                    RefreshActiveProviderModels();
                });
            }

            if (_closeButton != null)
            {
                _closeButton.clicked += Close;
            }

            if (_addModelButton != null)
            {
                _addModelButton.clicked += OpenCustomModelModal;
            }

            if (_deleteAllCustomButton != null)
            {
                _deleteAllCustomButton.clicked += HandleDeleteAllCustomModels;
            }

            if (_officialDocButton != null)
            {
                _officialDocButton.clicked += OpenOfficialDocs;
            }
        }

        private void SetupActionButtons()
        {
            if (_addModelButton != null)
            {
                _addModelButton.text = LocalizationManager.Get(LocalizationKeys.CUSTOM_MODEL_ADD_BUTTON);
            }

            if (_deleteAllCustomButton != null)
            {
                _deleteAllCustomButton.text = LocalizationManager.Get(LocalizationKeys.PROVIDER_DELETE_ALL_CUSTOM_MODELS);
            }

            if (_officialDocButton != null)
            {
                _officialDocButton.text = LocalizationManager.Get(LocalizationKeys.PROVIDER_OFFICIAL_DOCS);
            }
        }

        private void SetupWarningBanner()
        {
            if (_warningText != null)
            {
                _warningText.text = LocalizationManager.Get(LocalizationKeys.PROVIDER_MODEL_WARNING);
            }
        }

        private void SetupNoApiKeyWarning()
        {
            if (_noApiKeyText != null)
            {
                _noApiKeyText.text = LocalizationManager.Get(LocalizationKeys.API_KEY_INLINE_WARNING_MESSAGE_GENERIC);
            }

            if (_goToSettingsButton != null)
            {
                _goToSettingsButton.text = LocalizationManager.Get(LocalizationKeys.API_KEY_INLINE_WARNING_ACTION);
                _goToSettingsButton.clicked += HandleModelListWarningAction;
            }

            HideModelListWarning();
        }

        private void ShowNoApiKeyWarning(TProviderType providerType_)
        {
            string message = LocalizationManager.Get(LocalizationKeys.API_KEY_INLINE_WARNING_MESSAGE, providerType_.ToString());
            string actionText = LocalizationManager.Get(LocalizationKeys.API_KEY_INLINE_WARNING_ACTION);
            ShowModelListWarning(ModelListWarningType.API_KEY, message, actionText);
        }

        private void HideNoApiKeyWarning()
        {
            HideModelListWarning();
        }

        private void ShowCliPathWarning()
        {
            string message = LocalizationManager.Get(LocalizationKeys.CLI_PATH_INVALID_WARNING);
            string actionText = LocalizationManager.Get(LocalizationKeys.CLI_PATH_GO_TO_SETTINGS);
            ShowModelListWarning(ModelListWarningType.CLI_PATH, message, actionText);
        }

        private void ShowModelListWarning(ModelListWarningType warningType_, string message_, string actionText_)
        {
            _modelListWarningType = warningType_;

            if (_noApiKeyWarning != null)
            {
                _noApiKeyWarning.style.display = DisplayStyle.Flex;
            }

            if (_noApiKeyText != null)
            {
                _noApiKeyText.text = message_;
            }

            if (_goToSettingsButton != null)
            {
                _goToSettingsButton.text = actionText_;
            }
        }

        private void HideModelListWarning()
        {
            _modelListWarningType = ModelListWarningType.NONE;

            if (_noApiKeyWarning != null)
            {
                _noApiKeyWarning.style.display = DisplayStyle.None;
            }
        }

        private void HandleModelListWarningAction()
        {
            if (_modelListWarningType == ModelListWarningType.CLI_PATH)
            {
                HandleGoToCliPathSetting();
            }
            else if (_modelListWarningType == ModelListWarningType.API_KEY)
            {
                HandleGoToSettingApiKey();
            }
        }

        private void SetupApiKeySection()
        {
            if (_apiKeySection == null)
                return;

            // Initially hide the section
            _apiKeySection.style.display = DisplayStyle.None;
            _isEditingApiKey = false;

            if (_apiKeyOptionalRow != null)
            {
                _apiKeyOptionalRow.style.display = DisplayStyle.None;
            }

            if (_apiKeyOptionalToggle != null)
            {
                _apiKeyOptionalToggle.RegisterValueChangedCallback(evt_ =>
                {
                    HandleOptionalApiKeyToggle(evt_.newValue);
                });

                _apiKeyOptionalToggle.text = LocalizationManager.Get(LocalizationKeys.API_KEY_OPTIONAL_TOGGLE_LABEL);
            }

            // Setup field properties
            if (_apiKeyFieldReadonly != null)
            {
                _apiKeyFieldReadonly.isReadOnly = true;
                _apiKeyFieldReadonly.isDelayed = false;
            }

            if (_apiKeyFieldEditable != null)
            {
                _apiKeyFieldEditable.isReadOnly = false;
                _apiKeyFieldEditable.isDelayed = false;
            }

            // Register button callbacks
            if (_editApiKeyButton != null)
            {
                _editApiKeyButton.clicked += OnEditApiKeyClicked;
                _editApiKeyButton.text = LocalizationManager.Get(LocalizationKeys.API_KEY_EDIT_BUTTON);
            }

            if (_deleteApiKeyButton != null)
            {
                _deleteApiKeyButton.clicked += OnDeleteApiKeyClicked;
                _deleteApiKeyButton.text = LocalizationManager.Get(LocalizationKeys.API_KEY_DELETE_BUTTON);
            }

            if (_saveApiKeyButton != null)
            {
                _saveApiKeyButton.clicked += OnSaveApiKeyClicked;
                _saveApiKeyButton.text = LocalizationManager.Get(LocalizationKeys.API_KEY_SAVE_BUTTON);
            }

            if (_cancelEditButton != null)
            {
                _cancelEditButton.clicked += OnCancelEditClicked;
                _cancelEditButton.text = LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL);
            }

            if (_getApiKeyButton != null)
            {
                _getApiKeyButton.clicked += OnGetApiKeyClicked;
                _getApiKeyButton.text = LocalizationManager.Get(LocalizationKeys.API_KEY_GET_LINK);
            }

            if (_apiKeyLabel != null)
            {
                _apiKeyLabel.text = LocalizationManager.Get(LocalizationKeys.API_KEY_LABEL);
            }

            // Set initial mode to view
            UpdateApiKeySectionMode(false);
        }

        private void SetupCliPathSection()
        {
            if (_cliPathSection == null)
                return;

            _cliPathSection.style.display = DisplayStyle.None;

            if (_cliPathTitle != null)
            {
                _cliPathTitle.text = LocalizationManager.Get(LocalizationKeys.CLI_PATH_SECTION_TITLE);
            }

            if (_cliPathField != null)
            {
                _cliPathField.isReadOnly = false;
                _cliPathField.isDelayed = false;
                _cliPathField.RegisterValueChangedCallback(evt_ =>
                {
                    string newValue = evt_.newValue ?? string.Empty;
                    SetCliExecutablePath(newValue);
                    UpdateCliPathWarning(newValue);
                });
            }

            if (_cliPathAutoDetectButton != null)
            {
                _cliPathAutoDetectButton.text = LocalizationManager.Get(LocalizationKeys.CLI_PATH_AUTO_DETECT_BUTTON);
                _cliPathAutoDetectButton.clicked += () =>
                {
                    string detectedPath = FindCliExecutablePath(_activeProviderType);
                    if (!string.IsNullOrEmpty(detectedPath))
                    {
                        if (_cliPathField != null)
                            _cliPathField.SetValueWithoutNotify(detectedPath);

                        SetCliExecutablePath(detectedPath);
                        UpdateCliPathWarning(detectedPath);
                    }
                    else
                    {
                        UpdateCliPathWarning(_cliPathField != null ? _cliPathField.value : string.Empty);
                    }
                };
            }

            if (_cliPathBrowseButton != null)
            {
                _cliPathBrowseButton.text = LocalizationManager.Get(LocalizationKeys.CLI_PATH_BROWSE_BUTTON);
                _cliPathBrowseButton.clicked += () =>
                {
                    string path = EditorUtility.OpenFilePanel(
                        LocalizationManager.Get(LocalizationKeys.CLI_PATH_DIALOG_TITLE),
                        string.Empty,
                        string.Empty);

                    if (!string.IsNullOrEmpty(path))
                    {
                        if (_cliPathField != null)
                            _cliPathField.SetValueWithoutNotify(path);

                        SetCliExecutablePath(path);
                        UpdateCliPathWarning(path);
                    }
                };
            }

            if (_cliPathGuideButton != null)
            {
                _cliPathGuideButton.text = LocalizationManager.Get(LocalizationKeys.CLI_PATH_GUIDE_BUTTON);
                _cliPathGuideButton.clicked += () =>
                {
                    string url = GetCliInstallGuideUrl(_activeProviderType);
                    if (!string.IsNullOrEmpty(url))
                    {
                        Application.OpenURL(url);
                    }
                };
            }

            if (_noCliPathWarning != null)
            {
                _noCliPathWarning.style.display = DisplayStyle.None;
            }

            if (_noCliPathText != null)
            {
                _noCliPathText.text = LocalizationManager.Get(LocalizationKeys.CLI_PATH_INVALID_WARNING);
            }

            if (_goToCliPathButton != null)
            {
                _goToCliPathButton.text = LocalizationManager.Get(LocalizationKeys.CLI_PATH_GO_TO_SETTINGS);
                _goToCliPathButton.clicked += () =>
                {
                    HandleGoToCliPathSetting();
                };
            }
        }

        private void SetupNodePathSection()
        {
            if (_nodePathSection == null)
                return;

            _nodePathSection.style.display = DisplayStyle.None;

            if (_nodePathTitle != null)
            {
                _nodePathTitle.text = "Node.js Path (Optional)";
            }

            if (_nodePathField != null)
            {
                _nodePathField.isReadOnly = false;
                _nodePathField.isDelayed = false;
                _nodePathField.RegisterValueChangedCallback(evt_ =>
                {
                    string newValue = evt_.newValue ?? string.Empty;
                    SetNodeExecutablePath(newValue);
                });
            }

            if (_nodePathAutoDetectButton != null)
            {
                _nodePathAutoDetectButton.text = "Auto Detect";
                _nodePathAutoDetectButton.clicked += () =>
                {
                    string detectedPath = string.Empty;
                    if (_activeProviderType is ChatEditorProviderType chatProviderType)
                    {
                        switch (chatProviderType)
                        {
                            case ChatEditorProviderType.CLAUDE_CODE_CLI:
                            {
                                detectedPath = ClaudeCodeCliWrapper.FindNodeExecutablePath();
                                break;
                            }
                            case ChatEditorProviderType.CODEX_CLI:
                            {
                                detectedPath = CodexCliWrapper.FindNodeExecutablePath();
                                break;
                            }
                            case ChatEditorProviderType.GEMINI_CLI:
                            {
                                detectedPath = GeminiCliWrapper.FindNodeExecutablePath();
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(detectedPath))
                    {
                        if (_nodePathField != null)
                            _nodePathField.SetValueWithoutNotify(detectedPath);

                        SetNodeExecutablePath(detectedPath);
                    }
                };
            }

            if (_nodePathBrowseButton != null)
            {
                _nodePathBrowseButton.text = "Browse...";
                _nodePathBrowseButton.clicked += () =>
                {
                    string path = EditorUtility.OpenFilePanel(
                        "Select Node.js Executable",
                        string.Empty,
                        string.Empty);

                    if (!string.IsNullOrEmpty(path))
                    {
                        if (_nodePathField != null)
                            _nodePathField.SetValueWithoutNotify(path);

                        SetNodeExecutablePath(path);
                    }
                };
            }
        }

        private void UpdateNodePathSection(TProviderType providerType_)
        {
            if (_nodePathSection == null)
                return;

            if (_isAllProvidersActive || !IsCliProviderType(providerType_))
            {
                _nodePathSection.style.display = DisplayStyle.None;
                return;
            }

            _nodePathSection.style.display = DisplayStyle.Flex;

            string currentPath = GetNodeExecutablePath();
            if (_nodePathField != null)
                _nodePathField.SetValueWithoutNotify(currentPath);
        }

        private void UpdateCliPathSection(TProviderType providerType_)
        {
            if (_cliPathSection == null)
                return;

            if (_isAllProvidersActive || !IsCliProviderType(providerType_))
            {
                _cliPathSection.style.display = DisplayStyle.None;
                return;
            }

            _cliPathSection.style.display = DisplayStyle.Flex;

            string currentPath = GetCliExecutablePath();
            if (_cliPathField != null)
                _cliPathField.SetValueWithoutNotify(currentPath);

            UpdateCliPathWarning(currentPath);
        }

        private void UpdateModelListWarning(TProviderType providerType_)
        {
            if (_isAllProvidersActive)
            {
                HideModelListWarning();
                return;
            }

            bool isCliProvider = IsCliProviderType(providerType_);
            if (isCliProvider)
            {
                string cliPath = GetCliExecutablePath();
                bool hasCliPath = !string.IsNullOrWhiteSpace(cliPath);
                bool cliPathValid = hasCliPath && IsCliPathValid(cliPath);
                if (!cliPathValid)
                {
                    ShowCliPathWarning();
                    return;
                }

                bool useApiKey = !IsApiKeyOptionalProvider(providerType_) || IsApiKeyEnabledProvider(providerType_);
                if (useApiKey && !HasStoredApiKey(providerType_))
                {
                    ShowNoApiKeyWarning(providerType_);
                    return;
                }

                HideModelListWarning();
                return;
            }

            bool apiKeyRequired = !IsApiKeyOptionalProvider(providerType_) || IsApiKeyEnabledProvider(providerType_);
            if (apiKeyRequired && !HasStoredApiKey(providerType_))
            {
                ShowNoApiKeyWarning(providerType_);
                return;
            }

            HideModelListWarning();
        }

        private void UpdateCliPathWarning(string path_)
        {
            if (_noCliPathWarning == null)
                return;

            bool isValid = IsCliPathValid(path_);
            _noCliPathWarning.style.display = isValid ? DisplayStyle.None : DisplayStyle.Flex;

            if (_hasActiveProvider && !_isAllProvidersActive)
            {
                UpdateModelListWarning(_activeProviderType);
            }
        }

        private bool IsCliProviderType(TProviderType providerType_)
        {
            object providerTypeObj = providerType_;
            if (providerTypeObj is ChatEditorProviderType chatEditorProviderType)
            {
                return ChatEditorProviderTypeUtility.IsCliProvider(chatEditorProviderType);
            }

            return false;
        }

        private void HandleGoToCliPathSetting()
        {
            if (_apiKeySection == null)
                return;

            _apiKeySection.AddToClassList("pms-api-key-section--ping");
            _apiKeySection.schedule.Execute(() =>
            {
                _apiKeySection.RemoveFromClassList("pms-api-key-section--ping");
            }).StartingIn(1100);

            if (_apiKeySection != null)
            {
                _apiKeySection.Focus();
            }
        }

        private string GetCliInstallGuideUrl(TProviderType providerType_)
        {
            object providerTypeObj = providerType_;

            if (providerTypeObj is ChatEditorProviderType chatEditorProviderType)
            {
                return chatEditorProviderType switch
                {
                    ChatEditorProviderType.CODEX_CLI => "https://developers.openai.com/codex/cli/",
                    ChatEditorProviderType.CLAUDE_CODE_CLI => "https://code.claude.com/docs",
                    ChatEditorProviderType.GEMINI_CLI => "https://geminicli.com/docs/get-started/installation/",
                    _ => string.Empty
                };
            }

            return string.Empty;
        }

        private string GetCliExecutablePath()
        {
            if (_storage == null || !_hasActiveProvider)
                return string.Empty;

            if (_activeProviderType is ChatEditorProviderType chatProviderType)
                return EditorDataStorageKeys.GetCliExecutablePath(_storage, chatProviderType);

            return string.Empty;
        }

        private void SetCliExecutablePath(string path_)
        {
            if (_storage == null || !_hasActiveProvider)
                return;

            if (_activeProviderType is ChatEditorProviderType chatProviderType)
            {
                EditorDataStorageKeys.SetCliExecutablePath(_storage, chatProviderType, path_);

                // Update UI state
                bool hasAuth = HasApiKeyAvailable(_activeProviderType);
                UpdateProviderStatusAfterApiKeyChange(_activeProviderType, hasAuth);
                UpdateModelListWarning(_activeProviderType);

                _owner?.NotifyAuthChanged();
            }
        }

        private string GetNodeExecutablePath()
        {
            if (_storage == null || !_hasActiveProvider)
                return string.Empty;

            if (_activeProviderType is ChatEditorProviderType chatProviderType)
                return EditorDataStorageKeys.GetNodeExecutablePath(_storage, chatProviderType);

            return string.Empty;
        }

        private void SetNodeExecutablePath(string path_)
        {
            if (_storage == null || !_hasActiveProvider)
                return;

            if (_activeProviderType is ChatEditorProviderType chatProviderType)
            {
                EditorDataStorageKeys.SetNodeExecutablePath(_storage, chatProviderType, path_);

                // Update UI state
                bool hasAuth = HasApiKeyAvailable(_activeProviderType);
                UpdateProviderStatusAfterApiKeyChange(_activeProviderType, hasAuth);
                UpdateModelListWarning(_activeProviderType);

                _owner?.NotifyAuthChanged();
            }
        }

        private bool IsCliPathValid(string path_)
        {
            if (string.IsNullOrWhiteSpace(path_))
                return false;

            string trimmedPath = path_.Trim();
            if (Path.IsPathRooted(trimmedPath))
                return File.Exists(trimmedPath);

            return false;
        }

        private string FindCliExecutablePath(TProviderType providerType_)
        {
            if (providerType_ is ChatEditorProviderType chatProviderType)
            {
                switch (chatProviderType)
                {
                    case ChatEditorProviderType.CODEX_CLI:       return CodexCliWrapper.FindCodexExecutablePath();
                    case ChatEditorProviderType.CLAUDE_CODE_CLI: return ClaudeCodeCliWrapper.FindClaudeCodeExecutablePath();
                    case ChatEditorProviderType.GEMINI_CLI:      return GeminiCliWrapper.FindGeminiExecutablePath();
                }
            }
            return string.Empty;
        }

        private bool IsApiKeyOptionalProvider(TProviderType providerType_)
        {
            return _config.IsApiKeyOptional != null && _config.IsApiKeyOptional(providerType_);
        }

        private bool IsApiKeyEnabledProvider(TProviderType providerType_)
        {
            if (_config.IsApiKeyEnabled == null)
                return true;

            return _config.IsApiKeyEnabled(providerType_);
        }

        private void HandleOptionalApiKeyToggle(bool enabled_)
        {
            if (_config.SetApiKeyEnabled == null || !_hasActiveProvider)
                return;

            _config.SetApiKeyEnabled(_activeProviderType, enabled_);
            UpdateApiKeySection(_activeProviderType, HasApiKeyAvailable(_activeProviderType));
            UpdateProviderStatusAfterApiKeyChange(_activeProviderType, HasApiKeyAvailable(_activeProviderType));

            if (enabled_ && !HasStoredApiKey(_activeProviderType))
            {
                UpdateModelListWarning(_activeProviderType);
            }
            else
            {
                UpdateModelListWarning(_activeProviderType);
            }

            // Notify auth changed
            _owner?.NotifyAuthChanged();

            if (_config.NotifyProviderEnabledChanged != null)
            {
                bool hasKey = HasApiKeyAvailable(_activeProviderType);
                _config.NotifyProviderEnabledChanged(_activeProviderType, hasKey);
            }
        }

        private bool HasApiKeyAvailable(TProviderType providerType_)
        {
            return _config.HasApiKey != null && _config.HasApiKey(providerType_);
        }

        private bool HasStoredApiKey(TProviderType providerType_)
        {
            string apiKey = GetApiKeyForProvider(providerType_);
            return !string.IsNullOrEmpty(apiKey);
        }

        private void UpdateApiKeyStatusLabel(TProviderType providerType_, bool useApiKey_)
        {
            if (_apiKeyStatus == null)
                return;

            bool isOptional = IsApiKeyOptionalProvider(providerType_);
            bool hasKey = HasStoredApiKey(providerType_);

            _apiKeyStatus.RemoveFromClassList("pms-api-key-status--set");
            _apiKeyStatus.RemoveFromClassList("pms-api-key-status--missing");
            _apiKeyStatus.RemoveFromClassList("pms-api-key-status--optional");

            if (isOptional && !useApiKey_)
            {
                _apiKeyStatus.text = LocalizationManager.Get(LocalizationKeys.API_KEY_OPTIONAL_STATUS);
                _apiKeyStatus.AddToClassList("pms-api-key-status--optional");
            }
            else if (hasKey)
            {
                _apiKeyStatus.text = LocalizationManager.Get(LocalizationKeys.SETTINGS_API_KEY_SET);
                _apiKeyStatus.AddToClassList("pms-api-key-status--set");
            }
            else
            {
                _apiKeyStatus.text = LocalizationManager.Get(LocalizationKeys.SETTINGS_NO_API_KEY);
                _apiKeyStatus.AddToClassList("pms-api-key-status--missing");
            }

            _apiKeyStatus.style.display = DisplayStyle.Flex;
        }

        private void SetApiKeyControlsVisible(bool visible_)
        {
            DisplayStyle display = visible_ ? DisplayStyle.Flex : DisplayStyle.None;

            if (_apiKeyLabelRow != null)
                _apiKeyLabelRow.style.display = display;

            if (!visible_)
            {
                if (_apiKeyViewMode != null)
                    _apiKeyViewMode.style.display = DisplayStyle.None;

                if (_apiKeyEditMode != null)
                    _apiKeyEditMode.style.display = DisplayStyle.None;
            }
        }

        private void UpdateApiKeySection(TProviderType providerType_, bool hasKey_)
        {
            if (_apiKeySection == null)
                return;

            // Show section only for specific provider (not "All Providers")
            if (_isAllProvidersActive)
            {
                _apiKeySection.style.display = DisplayStyle.None;
                return;
            }

            _apiKeySection.style.display = DisplayStyle.Flex;

            // Update title
            if (_apiKeyTitle != null)
            {
                string providerName = providerType_.ToString();
                _apiKeyTitle.text = LocalizationManager.Get(LocalizationKeys.API_KEY_SECTION_TITLE, providerName);
            }

            bool isOptional = IsApiKeyOptionalProvider(providerType_);
            bool useApiKey = !isOptional || IsApiKeyEnabledProvider(providerType_);

            if (_apiKeyOptionalRow != null)
            {
                _apiKeyOptionalRow.style.display = isOptional ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_apiKeyOptionalToggle != null && isOptional)
            {
                _apiKeyOptionalToggle.SetValueWithoutNotify(useApiKey);
            }

            UpdateApiKeyStatusLabel(providerType_, useApiKey);

            // Load current API key
            _currentApiKey = GetApiKeyForProvider(providerType_);

            // Update Get API Key button URL
            UpdateGetApiKeyButton(providerType_);

            if (!useApiKey)
            {
                _isEditingApiKey = false;
                UpdateApiKeyFieldDisplay(string.Empty, false);
                SetApiKeyControlsVisible(false);
                return;
            }

            SetApiKeyControlsVisible(true);

            // Determine mode: if has key, show view mode; otherwise, show edit mode
            if (hasKey_ && !string.IsNullOrEmpty(_currentApiKey))
            {
                _isEditingApiKey = false;
                UpdateApiKeyFieldDisplay(_currentApiKey, true);
            }
            else
            {
                _isEditingApiKey = true;
                UpdateApiKeyFieldDisplay(string.Empty, false);
            }

            UpdateApiKeySectionMode(_isEditingApiKey);
        }

        public void ShowApiKeyInput()
        {
            HandleGoToSettingApiKey();
        }

        private void HandleGoToSettingApiKey()
        {
            if (_apiKeySection == null)
                return;

            _isEditingApiKey = true;
            UpdateApiKeySectionMode(true);
            _apiKeySection.AddToClassList("pms-api-key-section--ping");
            _apiKeySection.schedule.Execute(() =>
            {
                _apiKeySection.RemoveFromClassList("pms-api-key-section--ping");
            }).StartingIn(1100);

            if (_apiKeyFieldEditable != null)
            {
                _apiKeyFieldEditable.Focus();
            }
        }

        private string GetApiKeyForProvider(TProviderType providerType_)
        {
            if (_storage == null)
                return string.Empty;

            object providerTypeObj = providerType_;

            if (providerTypeObj is ChatProviderType chatProviderType)
            {
                return EditorDataStorageKeys.GetApiKey(_storage, chatProviderType);
            }
            else if (providerTypeObj is ChatEditorProviderType chatEditorProviderType)
            {
                return EditorDataStorageKeys.GetApiKey(_storage, chatEditorProviderType);
            }

            return string.Empty;
        }

        private string GetStorageKeyForProvider(TProviderType providerType_)
        {
            object providerTypeObj = providerType_;

            if (providerTypeObj is ChatProviderType chatProviderType)
            {
                return chatProviderType switch
                {
                    ChatProviderType.OPEN_AI => EditorDataStorageKeys.KEY_OPENAI,
                    ChatProviderType.GOOGLE => EditorDataStorageKeys.KEY_GOOGLE,
                    ChatProviderType.ANTHROPIC => EditorDataStorageKeys.KEY_ANTHROPIC,
                    ChatProviderType.HUGGING_FACE => EditorDataStorageKeys.KEY_HUGGINGFACE,
                    ChatProviderType.OPEN_ROUTER => EditorDataStorageKeys.KEY_OPENROUTER,
                    _ => string.Empty
                };
            }
            else if (providerTypeObj is ChatEditorProviderType chatEditorProviderType)
            {
                return chatEditorProviderType switch
                {
                    ChatEditorProviderType.OPEN_AI => EditorDataStorageKeys.KEY_OPENAI,
                    ChatEditorProviderType.GOOGLE => EditorDataStorageKeys.KEY_GOOGLE,
                    ChatEditorProviderType.ANTHROPIC => EditorDataStorageKeys.KEY_ANTHROPIC,
                    ChatEditorProviderType.HUGGING_FACE => EditorDataStorageKeys.KEY_HUGGINGFACE,
                    ChatEditorProviderType.OPEN_ROUTER => EditorDataStorageKeys.KEY_OPENROUTER,
                    ChatEditorProviderType.CODEX_CLI => EditorDataStorageKeys.KEY_CODEX,
                    _ => string.Empty
                };
            }

            return string.Empty;
        }

        private string GetApiKeyUrlForProvider(TProviderType providerType_)
        {
            object providerTypeObj = providerType_;

            if (providerTypeObj is ChatProviderType chatProviderType)
            {
                return chatProviderType switch
                {
                    ChatProviderType.OPEN_AI => "https://platform.openai.com/api-keys",
                    ChatProviderType.GOOGLE => "https://aistudio.google.com/app/apikey",
                    ChatProviderType.ANTHROPIC => "https://console.anthropic.com/settings/keys",
                    ChatProviderType.HUGGING_FACE => "https://huggingface.co/settings/tokens",
                    ChatProviderType.OPEN_ROUTER => "https://openrouter.ai/settings/keys",
                    _ => string.Empty
                };
            }
            else if (providerTypeObj is ChatEditorProviderType chatEditorProviderType)
            {
                return chatEditorProviderType switch
                {
                    ChatEditorProviderType.OPEN_AI => "https://platform.openai.com/api-keys",
                    ChatEditorProviderType.GOOGLE => "https://aistudio.google.com/app/apikey",
                    ChatEditorProviderType.ANTHROPIC => "https://console.anthropic.com/settings/keys",
                    ChatEditorProviderType.HUGGING_FACE => "https://huggingface.co/settings/tokens",
                    ChatEditorProviderType.OPEN_ROUTER => "https://openrouter.ai/settings/keys",
                    ChatEditorProviderType.CODEX_CLI => "https://platform.openai.com/api-keys",
                    ChatEditorProviderType.CLAUDE_CODE_CLI => "https://console.anthropic.com/settings/keys",
                    ChatEditorProviderType.GEMINI_CLI => "https://aistudio.google.com/app/apikey",
                    _ => string.Empty
                };
            }

            return string.Empty;
        }

        private void UpdateApiKeyFieldDisplay(string apiKey_, bool maskValue_)
        {
            if (maskValue_ && !string.IsNullOrEmpty(apiKey_))
            {
                // Mask the key with bullets
                string maskedKey = new string('•', Math.Min(apiKey_.Length, 32));
                if (_apiKeyFieldReadonly != null)
                {
                    _apiKeyFieldReadonly.value = maskedKey;
                }
            }
            else
            {
                if (_apiKeyFieldReadonly != null)
                {
                    _apiKeyFieldReadonly.value = string.Empty;
                }
                if (_apiKeyFieldEditable != null)
                {
                    _apiKeyFieldEditable.value = apiKey_;
                }
            }
        }

        private void UpdateApiKeySectionMode(bool isEditMode_)
        {
            if (_apiKeyViewMode != null)
            {
                _apiKeyViewMode.style.display = isEditMode_ ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_apiKeyEditMode != null)
            {
                _apiKeyEditMode.style.display = isEditMode_ ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateGetApiKeyButton(TProviderType providerType_)
        {
            if (_getApiKeyButton == null)
                return;

            string url = GetApiKeyUrlForProvider(providerType_);
            _getApiKeyButton.SetEnabled(!string.IsNullOrEmpty(url));

            if (!string.IsNullOrEmpty(url))
            {
                string providerName = providerType_.ToString();
                _getApiKeyButton.tooltip = LocalizationManager.Get(LocalizationKeys.SETTINGS_GET_API_KEY, providerName);
            }
        }

        private void UpdateProviderStatusAfterApiKeyChange(TProviderType providerType_, bool hasKey_)
        {
            // Update the provider row's status label in the provider list
            if (_providerRowByType.TryGetValue(providerType_, out VisualElement row))
            {
                Label statusLabel = row.Q<Label>(className: "pms-provider-status");
                if (statusLabel != null)
                {
                    UpdateProviderStatusLabel(statusLabel, providerType_);
                }

                // Update row disabled state
                if (hasKey_)
                {
                    row.RemoveFromClassList("pms-provider-row--disabled");
                }
                else
                {
                    row.AddToClassList("pms-provider-row--disabled");
                }
            }
        }

        private void UpdateProviderStatusLabel(Label statusLabel_, TProviderType providerType_)
        {
            if (statusLabel_ == null)
                return;

            bool isOptional = IsApiKeyOptionalProvider(providerType_);
            bool useApiKey = !isOptional || IsApiKeyEnabledProvider(providerType_);
            bool hasStoredKey = HasStoredApiKey(providerType_);

            statusLabel_.RemoveFromClassList("pms-provider-status--set");
            statusLabel_.RemoveFromClassList("pms-provider-status--missing");
            statusLabel_.RemoveFromClassList("pms-provider-status--optional");

            if (isOptional && !useApiKey)
            {
                statusLabel_.text = LocalizationManager.Get(LocalizationKeys.API_KEY_OPTIONAL_STATUS);
                statusLabel_.AddToClassList("pms-provider-status--optional");
            }
            else if (hasStoredKey)
            {
                statusLabel_.text = LocalizationManager.Get(LocalizationKeys.SETTINGS_API_KEY_SET);
                statusLabel_.AddToClassList("pms-provider-status--set");
            }
            else
            {
                statusLabel_.text = LocalizationManager.Get(LocalizationKeys.SETTINGS_NO_API_KEY);
                statusLabel_.AddToClassList("pms-provider-status--missing");
            }
        }

        private void OnEditApiKeyClicked()
        {
            _isEditingApiKey = true;
            string editingApiKey = _currentApiKey;

            if (_apiKeyFieldEditable != null)
            {
                _apiKeyFieldEditable.value = editingApiKey;
                _apiKeyFieldEditable.Focus();
            }

            UpdateApiKeySectionMode(true);
        }

        private void OnCancelEditClicked()
        {
            _isEditingApiKey = false;

            // Restore view mode
            bool hasKey = !string.IsNullOrEmpty(_currentApiKey);
            UpdateApiKeyFieldDisplay(_currentApiKey, hasKey);
            UpdateApiKeySectionMode(false);
        }

        private void OnSaveApiKeyClicked()
        {
            if (_apiKeyFieldEditable == null || _storage == null)
                return;

            string newApiKey = _apiKeyFieldEditable.value ?? string.Empty;

            // Validate non-empty
            if (string.IsNullOrWhiteSpace(newApiKey))
            {
                Debug.LogWarning("[ProviderModelSelectionPopup] API Key cannot be empty. Use Delete button to remove.");
                return;
            }

            // Get storage key for current provider
            string storageKey = GetStorageKeyForProvider(_activeProviderType);
            if (string.IsNullOrEmpty(storageKey))
            {
                Debug.LogError("[ProviderModelSelectionPopup] Cannot determine storage key for provider.");
                return;
            }

            // Save with encryption
            _storage.SetString(storageKey, newApiKey, true);

            // Update current key
            _currentApiKey = newApiKey;
            _isEditingApiKey = false;

            // Update UI
            UpdateApiKeyFieldDisplay(_currentApiKey, true);
            UpdateApiKeySectionMode(false);

            // Hide no-API-key warning if it was showing
            UpdateModelListWarning(_activeProviderType);

            // Update provider list status labels
            UpdateProviderStatusAfterApiKeyChange(_activeProviderType, true);

            // Notify auth changed
            _owner?.NotifyAuthChanged();
        }

        private void OnDeleteApiKeyClicked()
        {
            if (_storage == null)
                return;

            string providerName = _activeProviderType.ToString();

            // Confirmation dialog
            string title = LocalizationManager.Get(LocalizationKeys.API_KEY_DELETE_CONFIRM_TITLE);
            string message = LocalizationManager.Get(LocalizationKeys.API_KEY_DELETE_CONFIRM_MESSAGE, providerName);
            string confirmLabel = LocalizationManager.Get(LocalizationKeys.API_KEY_DELETE_BUTTON);
            string cancelLabel = LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL);

            if (!EditorUtility.DisplayDialog(title, message, confirmLabel, cancelLabel))
                return;

            // Get storage key
            string storageKey = GetStorageKeyForProvider(_activeProviderType);
            if (string.IsNullOrEmpty(storageKey))
            {
                Debug.LogError("[ProviderModelSelectionPopup] Cannot determine storage key for provider.");
                return;
            }

            // Delete from storage
            _storage.DeleteKey(storageKey);

            // Update state
            _currentApiKey = string.Empty;
            _isEditingApiKey = true;

            // Update UI to edit mode (empty field)
            UpdateApiKeyFieldDisplay(string.Empty, false);
            UpdateApiKeySectionMode(true);

            // Show no-API-key warning
            UpdateModelListWarning(_activeProviderType);

            // Update provider list status labels
            UpdateProviderStatusAfterApiKeyChange(_activeProviderType, false);

            // Notify auth changed
            _owner?.NotifyAuthChanged();
        }

        private void OnGetApiKeyClicked()
        {
            string url = GetApiKeyUrlForProvider(_activeProviderType);
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        private void UpdateUIElementsEnabledState(bool enabled_)
        {
            if (_modelSearchField != null)
            {
                _modelSearchField.SetEnabled(enabled_);
            }

            if (_sortDropdown != null)
            {
                _sortDropdown.SetEnabled(enabled_);
            }

            if (_addModelButton != null)
            {
                _addModelButton.SetEnabled(enabled_);
            }

            if (_deleteAllCustomButton != null)
            {
                _deleteAllCustomButton.SetEnabled(enabled_);
            }

            if (_officialDocButton != null)
            {
                _officialDocButton.SetEnabled(enabled_);
            }
        }

        private void OpenOfficialDocs()
        {
            if (!_hasActiveProvider || _isAllProvidersActive)
                return;

            string url = _config.GetModelsUrl != null
                ? _config.GetModelsUrl(_activeProviderType)
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

        private void OpenCustomModelModal()
        {
            if (_customModelModalView == null)
                return;

            if (!_hasActiveProvider || _isAllProvidersActive)
            {
                Debug.LogWarning("Please select a specific provider before adding a custom model.");
                return;
            }

            _customModelModalView.Open(_activeProviderType);
        }

        private void HandleDeleteAllCustomModels()
        {
            if (_config.RemoveCustomModel == null)
                return;

            string title = LocalizationManager.Get(LocalizationKeys.PROVIDER_DELETE_ALL_CUSTOM_MODELS_TITLE);
            string message = LocalizationManager.Get(LocalizationKeys.PROVIDER_DELETE_ALL_CUSTOM_MODELS_MESSAGE);
            string confirmLabel = LocalizationManager.Get(LocalizationKeys.PROVIDER_DELETE_CUSTOM_MODEL_CONFIRM);
            string cancelLabel = LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL);

            if (!EditorUtility.DisplayDialog(title, message, confirmLabel, cancelLabel))
                return;

            List<TProviderType> providerOrder = _owner.LoadProviderOrder();
            foreach (TProviderType providerType in providerOrder)
            {
                if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                    continue;

                List<TModelInfo> allModels = _config.GetAllModelInfos != null
                    ? _config.GetAllModelInfos(providerType)
                    : (_config.GetAllModelInfos != null
                        ? _config.GetAllModelInfos(providerType)
                        : new List<TModelInfo>());

                List<string> removedIds = new List<string>();
                foreach (TModelInfo modelInfo in allModels)
                {
                    if (!IsCustomModel(modelInfo))
                        continue;

                    string modelId = _config.GetModelIdFromInfo != null
                        ? _config.GetModelIdFromInfo(modelInfo)
                        : string.Empty;
                    if (string.IsNullOrEmpty(modelId))
                        continue;

                    _config.RemoveCustomModel(providerType, modelId);
                    removedIds.Add(modelId);
                }

                RemoveModelIdsFromSelection(providerType, removedIds);
            }

            _owner.UpdateSelectedModelChips();
            RefreshActiveProviderModels();
            if (_isAllProvidersActive)
            {
                BuildSelectedModelTabsForAll();
            }
            else if (_hasActiveProvider)
            {
                BuildSelectedModelTabs(_activeProviderType);
            }
            BuildHeaderSelectedChips();
            UpdateProviderSelectedCounts();
        }

        private void RemoveCustomModel(TProviderType providerType_, string modelId_)
        {
            if (_config.RemoveCustomModel == null || string.IsNullOrEmpty(modelId_))
                return;

            _config.RemoveCustomModel(providerType_, modelId_);
            RemoveModelIdsFromSelection(providerType_, new List<string> { modelId_ });

            _owner.UpdateSelectedModelChips();
            RefreshActiveProviderModels();
            if (_isAllProvidersActive)
            {
                BuildSelectedModelTabsForAll();
            }
            else if (_hasActiveProvider)
            {
                BuildSelectedModelTabs(providerType_);
            }
            BuildHeaderSelectedChips();
            UpdateProviderSelectedCount(providerType_);
        }

        private void RemoveModelIdsFromSelection(TProviderType providerType_, List<string> modelIds_)
        {
            if (modelIds_ == null || modelIds_.Count == 0)
                return;

            List<string> selectedModelIds = _owner.GetSelectedModelIdsInternal(providerType_);
            bool removedAny = false;
            foreach (string modelId in modelIds_)
            {
                if (selectedModelIds.Remove(modelId))
                {
                    removedAny = true;
                }
            }

            if (removedAny)
            {
                _owner.SetSelectedModelIdsInternal(providerType_, selectedModelIds);
            }
        }

        private void UpdateProviderSelectedCounts()
        {
            foreach (TProviderType providerType in _providerSelectedCountLabels.Keys)
            {
                UpdateProviderSelectedCount(providerType);
            }
        }

        private void BuildProviderList()
        {
            if (_providerListContainer == null || _providerAllContainer == null || _owner.PopupDragDropController == null)
                return;

            _providerListContainer.Clear();
            _providerAllContainer.Clear();
            _providerRowByType.Clear();
            _providerSelectedCountLabels.Clear();
            _allProvidersRow = null;

            if (MatchesProviderSearchText("All Providers"))
            {
                VisualElement allRow = CreateAllProvidersEntry();
                _providerAllContainer.Add(allRow);
                _allProvidersRow = allRow;
            }

            List<TProviderType> providerOrder = new List<TProviderType>(_owner.PopupDragDropController.ItemOrder);
            List<TProviderType> visibleProviders = new List<TProviderType>();

            foreach (TProviderType providerType in providerOrder)
            {
                if (!MatchesProviderSearch(providerType))
                    continue;

                VisualElement providerEntry = CreateProviderEntry(providerType);
                _owner.PopupDragDropController.SetupEntry(providerEntry, providerType);
                _providerListContainer.Add(providerEntry);
                visibleProviders.Add(providerType);
            }

            if (visibleProviders.Count == 0)
            {
                if (_modelPanelTitle != null)
                {
                    _modelPanelTitle.text = "Models";
                }

                if (_modelListContainer != null)
                {
                    _modelListContainer.Clear();
                }

                if (_modelSelectedTabsContainer != null)
                {
                    _modelSelectedTabsContainer.Clear();
                }

                _hasActiveProvider = false;
                _isAllProvidersActive = false;
                return;
            }

            if (_isAllProvidersActive)
            {
                UpdateActiveProviderVisuals();
                RefreshActiveProviderModels();
                return;
            }

            if (!_hasActiveProvider || !visibleProviders.Contains(_activeProviderType))
            {
                SetActiveProvider(visibleProviders[0], true);
            }
            else
            {
                RefreshActiveProviderModels();
            }
        }

        private VisualElement CreateProviderEntry(TProviderType providerType_)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("pms-provider-row");

            bool hasKey = _config.HasApiKey != null && _config.HasApiKey(providerType_);

            if (!hasKey)
            {
                row.AddToClassList("pms-provider-row--disabled");
            }

            VisualElement content = new VisualElement();
            content.AddToClassList("pms-provider-content");

            Label nameLabel = new Label(providerType_.ToString());
            nameLabel.AddToClassList("pms-provider-name");

            Label statusLabel = new Label();
            statusLabel.AddToClassList("pms-provider-status");
            UpdateProviderStatusLabel(statusLabel, providerType_);

            List<string> selectedIds = _owner.GetSelectedModelIdsInternal(providerType_);
            int selectedCount = selectedIds.Count;
            Label selectedCountLabel = new Label($"{selectedCount} selected");
            selectedCountLabel.AddToClassList("pms-provider-selected-count");
            _providerSelectedCountLabels[providerType_] = selectedCountLabel;

            content.Add(nameLabel);
            content.Add(statusLabel);
            content.Add(selectedCountLabel);
            row.Add(content);

            row.RegisterCallback<ClickEvent>(evt_ =>
            {
                if (!hasKey)
                {
                    UpdateModelListWarning(providerType_);
                    SetActiveProvider(providerType_, true);
                    ForceActiveProviderRow(row);
                    evt_.StopPropagation();
                    return;
                }

                HideModelListWarning();
                SetActiveProvider(providerType_, true);
                ForceActiveProviderRow(row);
            });

            if (EqualityComparer<TProviderType>.Default.Equals(providerType_, _activeProviderType) && _hasActiveProvider)
            {
                row.AddToClassList("pms-provider-row--active");
            }

            _providerRowByType[providerType_] = row;

            return row;
        }

        private VisualElement CreateAllProvidersEntry()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("pms-provider-row");
            row.AddToClassList("pms-provider-row--all");

            VisualElement content = new VisualElement();
            content.AddToClassList("pms-provider-content");

            Label nameLabel = new Label("All Providers");
            nameLabel.AddToClassList("pms-provider-name");
            nameLabel.AddToClassList("pms-provider-name--all");

            int selectedCount = GetAllSelectedEntries().Count;
            Label selectedCountLabel = new Label($"{selectedCount} selected");
            selectedCountLabel.AddToClassList("pms-provider-selected-count");

            content.Add(nameLabel);
            content.Add(selectedCountLabel);
            row.Add(content);

            row.RegisterCallback<ClickEvent>(evt_ =>
            {
                _isAllProvidersActive = true;
                _hasActiveProvider = false;
                HideModelListWarning();
                UpdateCliPathSection(default(TProviderType));
                UpdateNodePathSection(default(TProviderType));
                UpdateActiveProviderVisuals();
                RefreshActiveProviderModels();
                evt_.StopPropagation();
            });

            return row;
        }

        private void UpdateActiveProviderVisuals()
        {
            foreach (KeyValuePair<TProviderType, VisualElement> entry in _providerRowByType)
            {
                if (entry.Value != null)
                {
                    entry.Value.RemoveFromClassList("pms-provider-row--active");
                }
            }

            if (_allProvidersRow != null)
            {
                _allProvidersRow.RemoveFromClassList("pms-provider-row--active");
            }

            if (_isAllProvidersActive && _allProvidersRow != null)
            {
                _allProvidersRow.AddToClassList("pms-provider-row--active");
                return;
            }

            if (_providerRowByType.TryGetValue(_activeProviderType, out VisualElement activeRow) && activeRow != null)
            {
                activeRow.AddToClassList("pms-provider-row--active");
            }
        }

        private void ForceActiveProviderRow(VisualElement row_)
        {
            foreach (KeyValuePair<TProviderType, VisualElement> entry in _providerRowByType)
            {
                entry.Value?.RemoveFromClassList("pms-provider-row--active");
            }

            _allProvidersRow?.RemoveFromClassList("pms-provider-row--active");
            row_?.AddToClassList("pms-provider-row--active");
        }

        private void RefreshActiveProviderModels()
        {
            if (_isAllProvidersActive)
            {
                if (_modelPanelTitle != null)
                {
                    _modelPanelTitle.text = "Models";
                }

                UpdateUIElementsEnabledState(true);
                BuildAllProviderModels();
                BuildSelectedModelTabsForAll();
                BuildHeaderSelectedChips();
                return;
            }

            if (!_hasActiveProvider)
                return;

            if (_modelPanelTitle != null)
            {
                _modelPanelTitle.text = "Models";
            }

            BuildModelListForProvider(_activeProviderType);
            BuildSelectedModelTabs(_activeProviderType);
            BuildHeaderSelectedChips();
        }

        private void BuildModelListForProvider(TProviderType providerType_)
        {
            if (_modelListContainer == null)
                return;

            _modelListContainer.Clear();

            bool hasKey = _config.HasApiKey != null && _config.HasApiKey(providerType_);
            bool providerEnabled = hasKey;

            UpdateUIElementsEnabledState(hasKey);

            List<TModelInfo> modelInfos = _config.GetAllModelInfos != null
                ? _config.GetAllModelInfos(providerType_)
                : new List<TModelInfo>();

            if (modelInfos.Count == 0)
            {
                Label noModelsLabel = new Label(_config.GetNoModelsEnabledLabelText != null
                                                    ? _config.GetNoModelsEnabledLabelText(providerType_)
                                                    : string.Empty);
                noModelsLabel.AddToClassList("pms-model-empty");
                _modelListContainer.Add(noModelsLabel);
                return;
            }

            List<ModelEntry> entries = BuildModelEntriesForProvider(providerType_, providerEnabled, modelInfos);
            SortModelEntries(entries);
            SetupModelDragDrop(entries);

            foreach (ModelEntry entry in entries)
            {
                VisualElement card = CreateModelCard(
                    entry.providerType,
                    entry.modelInfo,
                    entry.modelId,
                    entry.displayName,
                    entry.description,
                    entry.isSelected,
                    entry.isProviderEnabled,
                    entry.isCustom);
                _modelDragDropController?.SetupEntry(card, entry.key);
                _modelListContainer.Add(card);
            }
        }

        private void BuildAllProviderModels()
        {
            if (_modelListContainer == null)
                return;

            _modelListContainer.Clear();

            List<TProviderType> providerOrder = _owner.LoadProviderOrder();
            List<TProviderType> sortedOrder = _owner.SortProvidersByEnabledState(providerOrder);

            List<ModelEntry> entries = new List<ModelEntry>();

            foreach (TProviderType providerType in sortedOrder)
            {
                if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                    continue;

                bool hasKey = _config.HasApiKey != null && _config.HasApiKey(providerType);
                bool providerEnabled = hasKey;

                List<TModelInfo> modelInfos = _config.GetAllModelInfos != null
                    ? _config.GetAllModelInfos(providerType)
                    : new List<TModelInfo>();

                List<ModelEntry> providerEntries = BuildModelEntriesForProvider(providerType, providerEnabled, modelInfos);
                entries.AddRange(providerEntries);
            }

            SortModelEntries(entries);
            SetupModelDragDrop(entries);

            foreach (ModelEntry entry in entries)
            {
                VisualElement card = CreateModelCard(
                    entry.providerType,
                    entry.modelInfo,
                    entry.modelId,
                    entry.displayName,
                    entry.description,
                    entry.isSelected,
                    entry.isProviderEnabled,
                    entry.isCustom);
                _modelDragDropController?.SetupEntry(card, entry.key);
                _modelListContainer.Add(card);
            }
        }

        private List<ModelEntry> BuildModelEntriesForProvider(TProviderType providerType_, bool providerEnabled_, List<TModelInfo> modelInfos_)
        {
            List<ModelEntry> entries = new List<ModelEntry>();
            List<string> selectedModelIds = _owner.GetSelectedModelIdsInternal(providerType_);

            foreach (TModelInfo modelInfo in modelInfos_)
            {
                string modelId = _config.GetModelIdFromInfo != null
                    ? _config.GetModelIdFromInfo(modelInfo)
                    : string.Empty;
                string displayName = _config.GetModelDisplayName != null
                    ? _config.GetModelDisplayName(modelInfo, modelId)
                    : modelId;
                string description = _config.GetModelDescription != null
                    ? _config.GetModelDescription(modelInfo, _owner.CurrentLanguageCode)
                    : string.Empty;

                string searchText = _owner.BuildModelSearchText(modelId, displayName, description, modelInfo);
                if (!_owner.MatchesSearch(searchText, _modelSearchText))
                {
                    continue;
                }

                bool isSelected = selectedModelIds.Contains(modelId);

                ModelEntry entry = new ModelEntry
                {
                    providerType = providerType_,
                    modelInfo = modelInfo,
                    modelId = modelId,
                    displayName = displayName,
                    description = description,
                    isSelected = isSelected,
                    isProviderEnabled = providerEnabled_,
                    isCustom = IsCustomModel(modelInfo),
                    priority = _owner.GetModelPriority(providerType_, modelId),
                    key = BuildModelKey(providerType_, modelId)
                };
                entries.Add(entry);
            }

            return entries;
        }

        private void SetupModelDragDrop(List<ModelEntry> entries_)
        {
            if (_modelListContainer == null)
                return;

            if (_modelDragDropController == null)
            {
                _modelDragDropController = new DragDropReorderController<string>(
                    _modelListContainer,
                    key_ => _modelDraggableByKey.TryGetValue(key_, out bool canDrag) && canDrag,
                    OnModelOrderChanged,
                    null);
            }

            _modelDragDropController.Clear();
            _modelDraggableByKey.Clear();
            _modelKeyMap.Clear();

            List<string> order = new List<string>();
            foreach (ModelEntry entry in entries_)
            {
                string key = entry.key;
                order.Add(key);
                _modelDraggableByKey[key] = entry.isProviderEnabled;
                _modelKeyMap[key] = (entry.providerType, entry.modelId);
            }

            _modelDragDropController.Initialize(order);
        }

        private void OnModelOrderChanged()
        {
            if (_modelDragDropController == null)
                return;

            IReadOnlyList<string> order = _modelDragDropController.ItemOrder;
            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                if (!_modelKeyMap.TryGetValue(key, out (TProviderType providerType, string modelId) data))
                    continue;

                int priority = _modelDragDropController.GetPriorityForIndex(i);
                _owner.SetModelPriority(data.providerType, data.modelId, priority);
            }

            RefreshActiveProviderModels();
            BuildHeaderSelectedChips();
            if (_isAllProvidersActive)
            {
                BuildSelectedModelTabsForAll();
            }
            else if (_hasActiveProvider)
            {
                BuildSelectedModelTabs(_activeProviderType);
            }
        }

        private string BuildModelKey(TProviderType providerType_, string modelId_)
        {
            return $"{providerType_}::{modelId_}";
        }

        private string GetContextWindowDisplay(TModelInfo modelInfo_)
        {
            if (modelInfo_ == null)
                return string.Empty;

            if (_config.GetContextWindowSize == null)
                return string.Empty;

            int contextWindowSize = _config.GetContextWindowSize(modelInfo_);
            if (contextWindowSize <= 0)
                return string.Empty;

            return FormatTokenCount(contextWindowSize);
        }

        private string GetMaxOutputDisplay(TModelInfo modelInfo_)
        {
            if (modelInfo_ == null)
                return string.Empty;

            if (_config.GetMaxOutputTokens == null)
                return string.Empty;

            int? maxOutputTokens = _config.GetMaxOutputTokens(modelInfo_);
            if (!maxOutputTokens.HasValue || maxOutputTokens.Value <= 0)
                return string.Empty;

            return FormatTokenCount(maxOutputTokens.Value);
        }

        private string FormatTokenCount(int tokens_)
        {
            if (tokens_ >= 1000000)
                return $"{tokens_ / 1000000.0:0.#}M tokens";
            if (tokens_ >= 1000)
                return $"{tokens_ / 1000.0:0.#}K tokens";
            return $"{tokens_} tokens";
        }

        private void SortModelEntries(List<ModelEntry> entries_)
        {
            entries_.Sort((a_, b_) =>
            {
                if (a_.isSelected != b_.isSelected)
                    return b_.isSelected.CompareTo(a_.isSelected);

                switch (_currentSortOrder)
                {
                    case ModelSortOrder.PRICE_ASCENDING:
                    {
                        double priceA = GetModelPrice(a_.modelInfo);
                        double priceB = GetModelPrice(b_.modelInfo);
                        int priceComparison = priceA.CompareTo(priceB);
                        if (priceComparison != 0)
                            return priceComparison;
                        break;
                    }

                    case ModelSortOrder.PRICE_DESCENDING:
                    {
                        double priceA = GetModelPrice(a_.modelInfo);
                        double priceB = GetModelPrice(b_.modelInfo);
                        int priceComparison = priceB.CompareTo(priceA);
                        if (priceComparison != 0)
                            return priceComparison;
                        break;
                    }

                    case ModelSortOrder.CONTEXT_DESCENDING:
                    {
                        int contextA = GetContextWindowSize(a_.modelInfo);
                        int contextB = GetContextWindowSize(b_.modelInfo);
                        int contextComparison = contextB.CompareTo(contextA);
                        if (contextComparison != 0)
                            return contextComparison;
                        break;
                    }
                }

                if (a_.priority != b_.priority)
                    return b_.priority.CompareTo(a_.priority);

                return string.Compare(a_.displayName, b_.displayName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private double GetModelPrice(TModelInfo modelInfo_)
        {
            if (modelInfo_ == null || _config.GetModelPrice == null)
                return 0;

            return _config.GetModelPrice(modelInfo_);
        }

        private int GetContextWindowSize(TModelInfo modelInfo_)
        {
            if (modelInfo_ == null || _config.GetContextWindowSize == null)
                return 0;

            return _config.GetContextWindowSize(modelInfo_);
        }

        private bool IsCustomModel(TModelInfo modelInfo_)
        {
            if (modelInfo_ == null)
                return false;

            if (modelInfo_ is ChatModelInfo chatModel)
                return chatModel.IsCustom;

            return false;
        }

        private VisualElement CreateModelCard(
            TProviderType providerType_,
            TModelInfo modelInfo_,
            string modelId_,
            string displayName_,
            string description_,
            bool isSelected_,
            bool isEnabled_,
            bool isCustom_)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("pms-model-card");
            if (isSelected_)
            {
                card.AddToClassList("pms-model-card--selected");
            }

            Toggle selectToggle = new Toggle();
            selectToggle.AddToClassList("pms-model-toggle");
            selectToggle.value = isSelected_;
            selectToggle.SetEnabled(isEnabled_);
            selectToggle.RegisterValueChangedCallback(evt_ =>
            {
                HandleModelToggleChanged(providerType_, modelId_, evt_.newValue);
                if (evt_.newValue)
                {
                    card.AddToClassList("pms-model-card--selected");
                }
                else
                {
                    card.RemoveFromClassList("pms-model-card--selected");
                }
                evt_.StopPropagation();
            });
            selectToggle.RegisterCallback<ClickEvent>(evt_ => { evt_.StopPropagation(); });

            VisualElement body = new VisualElement();
            body.AddToClassList("pms-model-body");

            VisualElement headerRow = new VisualElement();
            headerRow.AddToClassList("pms-model-row");

            Label nameLabel = new Label(displayName_);
            nameLabel.AddToClassList("pms-model-name");

            string providerAndModelID = _isAllProvidersActive ? $"{providerType_}::{modelId_}" : modelId_;
            Label idLabel = new Label(providerAndModelID);
            idLabel.AddToClassList("pms-model-id");

            VisualElement headerSpacer = new VisualElement();
            headerSpacer.AddToClassList("pms-model-row-spacer");

            string priceDisplay = _config.GetModelPriceDisplay != null
                ? _config.GetModelPriceDisplay(modelInfo_)
                : string.Empty;
            Label priceLabel = new Label(priceDisplay);
            priceLabel.AddToClassList("pms-model-meta");
            priceLabel.style.marginLeft = 8;

            VisualElement headerActions = new VisualElement();
            headerActions.AddToClassList("pms-model-header-actions");
            headerActions.Add(priceLabel);

            if (isCustom_)
            {
                Button deleteButton = new Button(() =>
                {
                    RemoveCustomModel(providerType_, modelId_);
                });
                deleteButton.text = LocalizationManager.Get(LocalizationKeys.PROVIDER_DELETE_CUSTOM_MODEL_BUTTON);
                deleteButton.AddToClassList("pms-model-delete-btn");
                deleteButton.RegisterCallback<ClickEvent>(evt_ => { evt_.StopPropagation(); });
                headerActions.Add(deleteButton);
            }

            headerRow.Add(nameLabel);
            if (isCustom_)
            {
                Label customBadge = new Label(LocalizationManager.Get(LocalizationKeys.PROVIDER_CUSTOM_MODEL_BADGE));
                customBadge.AddToClassList("pms-model-badge");
                customBadge.AddToClassList("pms-model-badge--custom");
                headerRow.Add(customBadge);
            }
            headerRow.Add(idLabel);
            headerRow.Add(headerSpacer);
            headerRow.Add(headerActions);

            VisualElement metaRow = new VisualElement();
            metaRow.AddToClassList("pms-model-row");
            metaRow.AddToClassList("pms-model-row--meta");

            Label descriptionLabel = new Label(description_);
            descriptionLabel.AddToClassList("pms-model-description");

            List<string> metaParts = new List<string>();

            string contextDisplay = GetContextWindowDisplay(modelInfo_);
            if (!string.IsNullOrEmpty(contextDisplay))
                metaParts.Add($"Ctx {contextDisplay}");

            string maxOutputDisplay = GetMaxOutputDisplay(modelInfo_);
            if (!string.IsNullOrEmpty(maxOutputDisplay))
                metaParts.Add($"Max {maxOutputDisplay}");

            Label metaLabel = new Label(string.Join(" | ", metaParts));
            metaLabel.AddToClassList("pms-model-meta");

            VisualElement bodySpacer = new VisualElement();
            bodySpacer.AddToClassList("pms-model-row-spacer");

            metaRow.Add(descriptionLabel);
            metaRow.Add(bodySpacer);
            metaRow.Add(metaLabel);

            body.Add(headerRow);
            body.Add(metaRow);

            VisualElement priorityContainer = new VisualElement();
            priorityContainer.AddToClassList("pms-model-priority-container");

            Label priorityLabel = new Label("Priority");
            priorityLabel.AddToClassList("pms-model-priority-label");

            IntegerField priorityField = new IntegerField();
            priorityField.AddToClassList("pms-model-priority-field");
            priorityField.isDelayed = true;
            priorityField.value = _owner.GetModelPriority(providerType_, modelId_);
            priorityField.SetEnabled(isEnabled_);
            priorityField.RegisterCallback<ClickEvent>(evt_ => { evt_.StopPropagation(); });
            priorityField.RegisterCallback<PointerDownEvent>(evt_ => { evt_.StopPropagation(); });
            priorityField.RegisterValueChangedCallback(evt_ =>
            {
                _owner.SetModelPriority(providerType_, modelId_, evt_.newValue);
                RefreshActiveProviderModels();
                BuildHeaderSelectedChips();
                if (_isAllProvidersActive)
                {
                    BuildSelectedModelTabsForAll();
                }
                else
                {
                    BuildSelectedModelTabs(providerType_);
                }
            });

            priorityContainer.Add(priorityLabel);
            priorityContainer.Add(priorityField);

            card.Add(selectToggle);
            card.Add(body);
            card.Add(priorityContainer);

            if (!string.IsNullOrEmpty(description_))
            {
                card.tooltip = description_;
            }

            card.RegisterCallback<ClickEvent>(evt_ =>
            {
                if (!isEnabled_)
                {
                    evt_.StopPropagation();
                    return;
                }

                selectToggle.value = !selectToggle.value;
                evt_.StopPropagation();
            });

            return card;
        }

        private void HandleModelToggleChanged(TProviderType providerType_, string modelId_, bool isSelected_)
        {
            List<string> selectedModelIds = _owner.GetSelectedModelIdsInternal(providerType_);

            if (isSelected_)
            {
                if (!selectedModelIds.Contains(modelId_))
                {
                    selectedModelIds.Add(modelId_);
                }
            }
            else
            {
                selectedModelIds.Remove(modelId_);
            }

            _owner.SetSelectedModelIdsInternal(providerType_, selectedModelIds);
            if (isSelected_)
            {
                AutoAssignPriorityForSelection(providerType_, modelId_, selectedModelIds);
            }
            _owner.UpdateSelectedModelChips();
            if (_isAllProvidersActive)
            {
                BuildSelectedModelTabsForAll();
            }
            else
            {
                BuildSelectedModelTabs(providerType_);
            }
            UpdateProviderSelectedCount(providerType_);
            BuildHeaderSelectedChips();
            RefreshActiveProviderModels();
        }

        private void AutoAssignPriorityForSelection(TProviderType providerType_, string modelId_, List<string> selectedModelIds_)
        {
            int currentPriority = _owner.GetModelPriority(providerType_, modelId_);
            if (currentPriority > 0)
                return;

            int minPriority = int.MaxValue;
            foreach (string modelId in selectedModelIds_)
            {
                int priority = _owner.GetModelPriority(providerType_, modelId);
                if (priority > 0 && priority < minPriority)
                    minPriority = priority;
            }

            int nextPriority = minPriority == int.MaxValue ? 100 : minPriority - 10;
            if (nextPriority < 1)
                nextPriority = 1;

            _owner.SetModelPriority(providerType_, modelId_, nextPriority);
        }

        private void BuildSelectedModelTabs(TProviderType providerType_)
        {
            if (_modelSelectedTabsContainer == null)
                return;

            _modelSelectedTabsContainer.Clear();

            List<string> selectedModelIds = _owner.GetSelectedModelIdsInternal(providerType_);
            List<string> sortedModelIds = SortModelIdsByPriority(providerType_, selectedModelIds);
            if (selectedModelIds.Count == 0)
            {
                Label emptyLabel = new Label(LocalizationManager.Get(LocalizationKeys.COMMON_MODEL_SELECTION));
                emptyLabel.AddToClassList("pms-model-chips-empty");
                _modelSelectedTabsContainer.Add(emptyLabel);
                return;
            }

            string primaryModelId = _owner.GetPrimaryModelId(providerType_, selectedModelIds);

            foreach (string modelId in sortedModelIds)
            {
                TModelInfo modelInfo = _config.GetModelInfo?.Invoke(providerType_, modelId);
                string displayName = _config.GetModelDisplayName != null
                    ? _config.GetModelDisplayName(modelInfo, modelId)
                    : modelId;

                VisualElement tab = new VisualElement();
                tab.AddToClassList("pms-model-chip");
                tab.AddToClassList("pms-model-chip--active");
                if (!string.IsNullOrEmpty(primaryModelId) && modelId == primaryModelId)
                {
                    tab.AddToClassList("pms-model-chip--active");
                }

                Label label = new Label(displayName);
                label.AddToClassList("pms-model-chip-label");

                Button closeButton = new Button(() =>
                {
                    HandleModelToggleChanged(providerType_, modelId, false);
                });
                closeButton.text = "x";
                closeButton.AddToClassList("pms-model-chip-close");
                closeButton.RegisterCallback<ClickEvent>(evt_ => { evt_.StopPropagation(); });

                tab.Add(label);
                tab.Add(closeButton);

                tab.RegisterCallback<ClickEvent>(evt_ =>
                {
                    SetPrimaryModel(providerType_, modelId);
                    evt_.StopPropagation();
                });

                _modelSelectedTabsContainer.Add(tab);
            }
        }

        private void BuildSelectedModelTabsForAll()
        {
            if (_modelSelectedTabsContainer == null)
                return;

            _modelSelectedTabsContainer.Clear();

            List<(TProviderType providerType, string modelId)> entries = GetAllSelectedEntries();
            SortSelectedEntriesByPriority(entries);
            if (entries.Count == 0)
            {
                Label emptyLabel = new Label(LocalizationManager.Get(LocalizationKeys.COMMON_MODEL_SELECTION));
                emptyLabel.AddToClassList("pms-model-chips-empty");
                _modelSelectedTabsContainer.Add(emptyLabel);
                return;
            }

            foreach ((TProviderType providerType, string modelId) entry in entries)
            {
                TModelInfo modelInfo = _config.GetModelInfo?.Invoke(entry.providerType, entry.modelId);
                string displayName = _config.GetModelDisplayName != null
                    ? _config.GetModelDisplayName(modelInfo, entry.modelId)
                    : entry.modelId;
                string labelText = $"{entry.providerType} - {displayName}";

                VisualElement tab = new VisualElement();
                tab.AddToClassList("pms-model-chip");
                tab.AddToClassList("pms-model-chip--active");

                Label label = new Label(labelText);
                label.AddToClassList("pms-model-chip-label");

                Button closeButton = new Button(() =>
                {
                    HandleModelToggleChanged(entry.providerType, entry.modelId, false);
                });
                closeButton.text = "x";
                closeButton.AddToClassList("pms-model-chip-close");
                closeButton.RegisterCallback<ClickEvent>(evt_ => { evt_.StopPropagation(); });

                tab.Add(label);
                tab.Add(closeButton);

                tab.RegisterCallback<ClickEvent>(evt_ =>
                {
                    SetPrimaryModel(entry.providerType, entry.modelId);
                    evt_.StopPropagation();
                });

                _modelSelectedTabsContainer.Add(tab);
            }
        }

        private void SetPrimaryModel(TProviderType providerType_, string modelId_)
        {
            if (string.IsNullOrEmpty(modelId_))
                return;

            _owner.SetPrimaryModelId(providerType_, modelId_);

            if (_isAllProvidersActive)
            {
                BuildSelectedModelTabsForAll();
            }
            else
            {
                BuildSelectedModelTabs(providerType_);
            }
        }

        private void UpdateProviderSelectedCount(TProviderType providerType_)
        {
            if (_providerSelectedCountLabels.TryGetValue(providerType_, out Label label) && label != null)
            {
                int selectedCount = _owner.GetSelectedModelIdsInternal(providerType_).Count;
                label.text = $"{selectedCount} selected";
            }

            if (_allProvidersRow != null)
            {
                Label allCountLabel = _allProvidersRow.Q<Label>(className: "pms-provider-selected-count");
                if (allCountLabel != null)
                {
                    int totalSelected = GetAllSelectedEntries().Count;
                    allCountLabel.text = $"{totalSelected} selected";
                }
            }
        }

        private void BuildHeaderSelectedChips()
        {
            if (_headerSelectedTabsContainer == null)
                return;

            _headerSelectedTabsContainer.Clear();

            List<(TProviderType providerType, string modelId)> entries = GetAllSelectedEntries();
            SortSelectedEntriesByPriority(entries);
            foreach ((TProviderType providerType, string modelId) entry in entries)
            {
                TModelInfo modelInfo = _config.GetModelInfo?.Invoke(entry.providerType, entry.modelId);
                string displayName = _config.GetModelDisplayName != null
                    ? _config.GetModelDisplayName(modelInfo, entry.modelId)
                    : entry.modelId;
                string labelText = $"{entry.providerType} - {displayName}";

                VisualElement chip = new VisualElement();
                chip.AddToClassList("pms-model-chip");
                chip.AddToClassList("pms-model-chip--active");

                Label label = new Label(labelText);
                label.AddToClassList("pms-model-chip-label");
                chip.Add(label);

                _headerSelectedTabsContainer.Add(chip);
            }
        }

        private List<(TProviderType providerType, string modelId)> GetAllSelectedEntries()
        {
            List<(TProviderType providerType, string modelId)> entries =
                new List<(TProviderType providerType, string modelId)>();

            List<TProviderType> providerOrder = _owner.LoadProviderOrder();
            foreach (TProviderType providerType in providerOrder)
            {
                if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                    continue;

                List<string> modelIds = _owner.GetSelectedModelIdsInternal(providerType);
                foreach (string modelId in modelIds)
                {
                    entries.Add((providerType, modelId));
                }
            }

            return entries;
        }

        private List<string> SortModelIdsByPriority(TProviderType providerType_, List<string> modelIds_)
        {
            List<string> sorted = new List<string>(modelIds_);
            sorted.Sort((a_, b_) =>
            {
                int priorityA = _owner.GetModelPriority(providerType_, a_);
                int priorityB = _owner.GetModelPriority(providerType_, b_);

                if (priorityA != priorityB)
                    return priorityB.CompareTo(priorityA);

                return string.Compare(a_, b_, StringComparison.OrdinalIgnoreCase);
            });

            return sorted;
        }

        private void SortSelectedEntriesByPriority(List<(TProviderType providerType, string modelId)> entries_)
        {
            entries_.Sort((a_, b_) =>
            {
                int priorityA = _owner.GetModelPriority(a_.providerType, a_.modelId);
                int priorityB = _owner.GetModelPriority(b_.providerType, b_.modelId);

                if (priorityA != priorityB)
                    return priorityB.CompareTo(priorityA);

                string providerA = a_.providerType.ToString();
                string providerB = b_.providerType.ToString();
                int providerCompare = string.Compare(providerA, providerB, StringComparison.OrdinalIgnoreCase);
                if (providerCompare != 0)
                    return providerCompare;

                return string.Compare(a_.modelId, b_.modelId, StringComparison.OrdinalIgnoreCase);
            });
        }

        private void OnPopupProviderOrderChanged()
        {
            if (_owner.PopupDragDropController == null || _owner.DragDropController == null)
                return;

            List<TProviderType> newOrder = new List<TProviderType>(_owner.PopupDragDropController.ItemOrder);
            _owner.DragDropController.Initialize(newOrder);

            _owner.SaveProviderOrder();
            _owner.UpdatePrioritiesFromOrder();
            _owner.UpdateSelectedModelChips();

            _config.NotifyProviderOrderChanged?.Invoke();

            BuildProviderList();
            UpdateActiveProviderVisuals();
        }

        private bool MatchesProviderSearch(TProviderType providerType_)
        {
            string searchText = _providerSearchText ?? string.Empty;
            if (string.IsNullOrEmpty(searchText))
                return true;

            return providerType_.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool MatchesProviderSearchText(string providerName_)
        {
            string searchText = _providerSearchText ?? string.Empty;
            if (string.IsNullOrEmpty(searchText))
                return true;

            return providerName_.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}