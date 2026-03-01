using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ProviderModelSelectionElement<TProviderType, TModelInfo> : VisualElement
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        public event Action OnAuthChanged;

        private const string SELECTED_MODELS_CHIPS_NAME = "selected-models-chips";

        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;
        private readonly VisualElement _root;
        private readonly ProviderModelSelectionPopupCoordinator<TProviderType, TModelInfo> _popupCoordinator;
        private readonly ProviderModelSelectionStateService<TProviderType, TModelInfo> _stateService;
        private readonly ProviderModelSelectionOrderService<TProviderType, TModelInfo> _orderService;
        private readonly ProviderSelectedModelChipsRenderer<TProviderType, TModelInfo> _chipsRenderer;
        private readonly ProviderModelSelectionUtilityService<TProviderType, TModelInfo> _utilityService;

        public EditorDataStorage Storage { get; private set; }
        public string CurrentLanguageCode { get; private set; }

        public Button ModelSelectionButton { get; private set; }
        public VisualElement SelectedModelsChipsContainer { get; private set; }
        public VisualElement ModelSelectionPopup { get; internal set; }
        public VisualElement PopupProviderList { get; internal set; }
        public VisualElement PopupOverlay { get; internal set; }

        public Dictionary<TProviderType, Toggle> ProviderToggles { get; } = new Dictionary<TProviderType, Toggle>();
        public Dictionary<TProviderType, IntegerField> ProviderPriorities { get; } = new Dictionary<TProviderType, IntegerField>();
        public Dictionary<TProviderType, DropdownField> ProviderModels { get; } = new Dictionary<TProviderType, DropdownField>();

        public Dictionary<TProviderType, Dictionary<string, string>> ModelDisplayToId { get; } =
            new Dictionary<TProviderType, Dictionary<string, string>>();

        public DragDropReorderController<TProviderType> DragDropController { get; private set; }
        public DragDropReorderController<TProviderType> PopupDragDropController { get; internal set; }

        internal ProviderModelSelectionConfig<TProviderType, TModelInfo> Config => _config;
        internal VisualElement RootElement => _root;

        public EditorProviderManagerInterface<TProviderType, TModelInfo> Manager { get; private set; }

        public ProviderModelSelectionElement(
            VisualElement root_,
            ProviderModelSelectionConfig<TProviderType, TModelInfo> config_,
            EditorProviderManagerInterface<TProviderType, TModelInfo> manager_ = null)
        {
            _root = root_ ?? this;
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
            Manager = manager_;
            _stateService = new ProviderModelSelectionStateService<TProviderType, TModelInfo>(_config);
            _orderService = new ProviderModelSelectionOrderService<TProviderType, TModelInfo>(_config);
            _chipsRenderer = new ProviderSelectedModelChipsRenderer<TProviderType, TModelInfo>(_config);
            _utilityService = new ProviderModelSelectionUtilityService<TProviderType, TModelInfo>(_config);
            _popupCoordinator = new ProviderModelSelectionPopupCoordinator<TProviderType, TModelInfo>(this, _config);
        }

        public void Initialize(EditorDataStorage storage_, string languageCode_)
        {
            Storage = storage_;
            CurrentLanguageCode = languageCode_;
        }

        public void SetModelState(EditorProviderManagerInterface<TProviderType, TModelInfo> manager_)
        {
            Manager = manager_;
        }

        public void SetupUI(string modelSelectionButtonName_)
        {
            ModelSelectionButton = _root.Q<Button>(modelSelectionButtonName_);
            SelectedModelsChipsContainer = _root.Q<VisualElement>(SELECTED_MODELS_CHIPS_NAME);

            if (ModelSelectionButton != null)
            {
                ModelSelectionButton.clicked += ToggleModelSelectionPopup;
            }
        }

        public void UpdateLanguage(string languageCode_)
        {
            CurrentLanguageCode = languageCode_;
            UpdateSelectedModelChips();
            _popupCoordinator.RefreshLanguage(languageCode_);
        }

        public void OnHide()
        {
            _popupCoordinator.Close();
        }

        public void InitializeDragDropController(
            VisualElement listRoot_,
            Func<TProviderType, bool> canReorder_,
            Action onOrderChanged_,
            Action<TProviderType> showApiKeyWarning_)
        {
            DragDropController = new DragDropReorderController<TProviderType>(
                listRoot_,
                canReorder_,
                onOrderChanged_,
                showApiKeyWarning_);
        }

        public List<TProviderType> LoadProviderOrder()
        {
            return _orderService.LoadProviderOrder(Manager, Storage);
        }

        public void SaveProviderOrder()
        {
            _orderService.SaveProviderOrder(DragDropController, Storage);
        }

        public void UpdatePrioritiesFromOrder()
        {
            _orderService.UpdatePrioritiesFromOrder(DragDropController, ProviderPriorities);
        }

        public List<TProviderType> SortProvidersByEnabledState(List<TProviderType> providerOrder_)
        {
            return _orderService.SortProvidersByEnabledState(providerOrder_, HasProviderAuth, IsProviderEnabled);
        }

        public void UpdateSelectedModelChips()
        {
            int selectedCount = _chipsRenderer.Render(
                SelectedModelsChipsContainer,
                DragDropController,
                CurrentLanguageCode,
                HasProviderAuth,
                IsProviderEnabled,
                GetSelectedModelIdsInternal,
                GetDisabledModelIds,
                ToggleModelActiveState,
                UpdateSelectedModelChips);

            UpdateSelectionButtonText(selectedCount);
        }

        public void Dispose()
        {
            if (ModelSelectionButton != null)
            {
                ModelSelectionButton.clicked -= ToggleModelSelectionPopup;
            }

            _popupCoordinator.Close();
        }

        public int GetModelPriority(TProviderType providerType_, string modelId_)
        {
            if (Storage == null)
                return 0;

            string key = EditorDataStorageKeys.GetModelPriorityKey(providerType_.ToString(), modelId_);
            if (string.IsNullOrEmpty(key))
                return 0;

            return Storage.GetInt(key, 0);
        }

        public void SetModelPriority(TProviderType providerType_, string modelId_, int priority_)
        {
            if (Storage == null)
                return;

            string key = EditorDataStorageKeys.GetModelPriorityKey(providerType_.ToString(), modelId_);
            if (string.IsNullOrEmpty(key))
                return;

            Storage.SetInt(key, priority_);
            Storage.Save();
        }

        public string GetModelIdFromDisplay(TProviderType providerType_, string displayText_)
        {
            if (displayText_ == null)
            {
                return null;
            }

            if (ModelDisplayToId.TryGetValue(providerType_, out Dictionary<string, string> mapping))
            {
                if (mapping.TryGetValue(displayText_, out string modelId))
                {
                    return modelId;
                }
            }

            return displayText_;
        }

        private void UpdateSelectionButtonText(int selectedCount_)
        {
            if (ModelSelectionButton == null)
                return;

            ModelSelectionButton.text = _config.GetSelectionButtonText != null
                ? _config.GetSelectionButtonText(selectedCount_)
                : $"({selectedCount_})";
        }

        private void ToggleModelSelectionPopup()
        {
            _popupCoordinator.Toggle(Storage);
        }

        private void ShowModelSelectionPopup()
        {
            _popupCoordinator.Show(Storage);
        }

        private void SetActiveProviderInPopup(TProviderType providerType_, bool rebuildModels_)
        {
            _popupCoordinator.SetActiveProvider(providerType_, rebuildModels_);
        }

        public void ShowApiKeyInputForFirstProviderWithoutKey()
        {
            _popupCoordinator.ShowApiKeyInputForFirstProviderWithoutKey(Storage, DragDropController, HasProviderAuth);
        }

        internal void NotifyAuthChanged()
        {
            OnAuthChanged?.Invoke();
        }

        internal List<string> GetSelectedModelIdsInternal(TProviderType providerType_)
        {
            return _stateService.GetSelectedModelIds(providerType_);
        }

        internal void SetSelectedModelIdsInternal(TProviderType providerType_, List<string> modelIds_)
        {
            _stateService.SetSelectedModelIds(providerType_, modelIds_);
        }

        internal string GetPrimaryModelId(TProviderType providerType_, List<string> modelIds_)
        {
            return _stateService.GetPrimaryModelId(providerType_, modelIds_);
        }

        internal List<string> NormalizeModelIdList(TProviderType providerType_, List<string> modelIds_)
        {
            return _stateService.NormalizeModelIdList(providerType_, modelIds_);
        }

        internal void SetPrimaryModelId(TProviderType providerType_, string modelId_)
        {
            _stateService.SetPrimaryModelId(providerType_, modelId_);
        }

        internal VisualElement GetRootVisualContainer()
        {
            return _utilityService.GetRootVisualContainer(_root);
        }

        internal string BuildModelSearchText(string modelId_, string displayName_, string description_, TModelInfo modelInfo_)
        {
            return _utilityService.BuildModelSearchText(modelId_, displayName_, description_, modelInfo_, CurrentLanguageCode);
        }

        internal bool MatchesSearch(string haystack_, string needle_)
        {
            return _utilityService.MatchesSearch(haystack_, needle_);
        }

        private bool HasProviderAuth(TProviderType providerType_)
        {
            return _config.HasProviderAuth != null && _config.HasProviderAuth(providerType_);
        }

        private bool IsProviderEnabled(TProviderType providerType_)
        {
            if (_config.IsProviderEnabled == null)
                return true;

            return _config.IsProviderEnabled(providerType_);
        }

        private HashSet<string> GetDisabledModelIds(TProviderType providerType_)
        {
            return _stateService.GetDisabledModelIds(providerType_);
        }

        private void ToggleModelActiveState(TProviderType providerType_, string modelId_)
        {
            _stateService.ToggleModelActiveState(providerType_, modelId_);
        }
    }
}
