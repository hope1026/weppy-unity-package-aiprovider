using System;

namespace Weppy.AIProvider.Editor
{
    public class ProviderModelSelectionPopupCoordinator<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        private readonly ProviderModelSelectionElement<TProviderType, TModelInfo> _owner;
        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;

        private ProviderModelSelectionPopupView<TProviderType, TModelInfo> _popupView;

        public ProviderModelSelectionPopupCoordinator(
            ProviderModelSelectionElement<TProviderType, TModelInfo> owner_,
            ProviderModelSelectionConfig<TProviderType, TModelInfo> config_)
        {
            _owner = owner_ ?? throw new ArgumentNullException(nameof(owner_));
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
        }

        public void Toggle(EditorDataStorage storage_)
        {
            EnsurePopupView(storage_);
            _popupView?.Toggle();
        }

        public void Show(EditorDataStorage storage_)
        {
            EnsurePopupView(storage_);
            _popupView?.Show();
        }

        public void SetActiveProvider(TProviderType providerType_, bool rebuildModels_)
        {
            _popupView?.SetActiveProvider(providerType_, rebuildModels_);
        }

        public void RefreshLanguage(string languageCode_)
        {
            _popupView?.RefreshLanguage(languageCode_);
        }

        public void Close()
        {
            _popupView?.Close();
        }

        public void ShowApiKeyInputForFirstProviderWithoutKey(
            EditorDataStorage storage_,
            DragDropReorderController<TProviderType> dragDropController_,
            Func<TProviderType, bool> hasProviderAuth_)
        {
            EnsurePopupView(storage_);
            if (_popupView == null)
                return;

            TProviderType firstProviderWithoutAuth = default;
            bool found = false;

            if (dragDropController_ != null && dragDropController_.ItemOrder != null)
            {
                foreach (TProviderType providerType in dragDropController_.ItemOrder)
                {
                    if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                        continue;

                    bool hasProviderAuth = hasProviderAuth_ != null && hasProviderAuth_(providerType);
                    if (!hasProviderAuth)
                    {
                        firstProviderWithoutAuth = providerType;
                        found = true;
                        break;
                    }
                }
            }

            _popupView.Show();

            if (found)
            {
                _popupView.SetActiveProvider(firstProviderWithoutAuth, true);
            }

            _popupView.ShowApiKeyInput();
        }

        private void EnsurePopupView(EditorDataStorage storage_)
        {
            if (_popupView != null)
                return;

            _popupView = new ProviderModelSelectionPopupView<TProviderType, TModelInfo>(_owner, _config, storage_);
        }
    }
}
