using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ProviderEditorWindow : EditorWindow
    {
        private static readonly string EDITOR_PATH = EditorPaths.EDITOR_WINDOW_PATH;

        private enum TabType
        {
            SETTINGS,
            CHAT,
            IMAGE_GENERATION,
            BACKGROUND_REMOVAL
        }

        private EditorDataStorage _storage;
        private SettingsView _settingsView;
        private ChatView _chatView;
        private ImageView _imageGenerationView;
        private BgRemovalView _bgRemovalView;

        private Button _settingsTab;
        private Button _chatTab;
        private Button _imageTab;
        private Button _bgRemovalTab;
        private VisualElement _settingsContent;
        private VisualElement _chatContent;
        private VisualElement _imageContent;
        private VisualElement _bgRemovalContent;
        private Label _statusText;

        [MenuItem("Window/Weppy/AI Provider")]
        public static void ShowWindow()
        {
            ProviderEditorWindow window = GetWindow<ProviderEditorWindow>();
            window.titleContent = new GUIContent("AI Provider");
            window.minSize = new Vector2(500, 700);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedUI;
            
            _chatView?.SaveHistoryIfNeeded();
            _imageGenerationView?.SaveHistoryIfNeeded();
            _bgRemovalView?.SaveHistoryIfNeeded();

            _chatView?.Dispose();
            _imageGenerationView?.Dispose();
            _bgRemovalView?.Dispose();
        }

        private void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EDITOR_PATH + "ProviderEditorWindow.uxml");
            if (visualTree != null)
            {
                visualTree.CloneTree(root);
            }
            else
            {
                Debug.LogError("[AIProvider Image] Could not load ProviderEditorWindow.uxml");
                return;
            }

            StyleSheet themeStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorPaths.THEME_STYLES_PATH);
            if (themeStyles != null)
            {
                root.styleSheets.Add(themeStyles);
            }

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EDITOR_PATH + "ProviderEditorWindow.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            InitializeStorage();
            SetupUIReferences(root);
            CreateViews();
            SetupEventHandlers();
        }

        private void InitializeStorage()
        {
            _storage = new EditorDataStorage();
            _storage.Load();
        }

        private void SetupUIReferences(VisualElement root_)
        {
            _settingsTab = root_.Q<Button>("settings-tab");
            _chatTab = root_.Q<Button>("chat-tab");
            _imageTab = root_.Q<Button>("image-tab");
            _bgRemovalTab = root_.Q<Button>("bgremoval-tab");
            _settingsContent = root_.Q<VisualElement>("settings-content");
            _chatContent = root_.Q<VisualElement>("chat-content");
            _imageContent = root_.Q<VisualElement>("image-content");
            _bgRemovalContent = root_.Q<VisualElement>("bgremoval-content");
            _statusText = root_.Q<Label>("status-text");

            UpdateLocalizedUI();
            LocalizationManager.OnLanguageChanged += UpdateLocalizedUI;
        }

        private void UpdateLocalizedUI()
        {
            if (_chatTab != null)
                _chatTab.text = LocalizationManager.Get(LocalizationKeys.MAIN_TAB_CHAT);
            if (_imageTab != null)
                _imageTab.text = LocalizationManager.Get(LocalizationKeys.MAIN_TAB_IMAGE_GENERATION);
            if (_bgRemovalTab != null)
                _bgRemovalTab.text = LocalizationManager.Get(LocalizationKeys.MAIN_TAB_BACKGROUND_REMOVAL);
            if (_settingsTab != null)
                _settingsTab.text = LocalizationManager.Get(LocalizationKeys.MAIN_TAB_SETTINGS);
            if (_statusText != null && string.IsNullOrEmpty(_statusText.text))
                _statusText.text = LocalizationManager.Get(LocalizationKeys.STATUS_READY);
        }

        private void CreateViews()
        {
            _settingsView = new SettingsView(_storage);
            _settingsContent.Add(_settingsView);

            _chatView = new ChatView(_storage);
            _chatContent.Add(_chatView);
            
            _imageGenerationView = new ImageView(_storage);
            _imageContent.Add(_imageGenerationView);

            _bgRemovalView = new BgRemovalView(_storage);
            _bgRemovalContent.Add(_bgRemovalView);

            RestoreTabState();
        }

        private void RestoreTabState()
        {
            int savedTabIndex = _storage.GetInt(EditorDataStorageKeys.KEY_MAIN_TAB, 0);
            TabType savedTab = (TabType)savedTabIndex;
            SwitchTab(savedTab);
        }

        private void SetupEventHandlers()
        {
            _settingsTab.clicked += () => SwitchTab(TabType.SETTINGS);
            _chatTab.clicked += () => SwitchTab(TabType.CHAT);
            _imageTab.clicked += () => SwitchTab(TabType.IMAGE_GENERATION);
            _bgRemovalTab.clicked += () => SwitchTab(TabType.BACKGROUND_REMOVAL);

            _settingsView.OnStatusChanged += SetStatus;
            _settingsView.OnSettingsChanged += OnSettingsChanged;

            _chatView.OnStatusChanged += SetStatus;
            _chatView.OnNavigateToSettingsRequested += () => SwitchTab(TabType.SETTINGS);
            
            _imageGenerationView.OnStatusChanged += SetStatus;
            _imageGenerationView.OnNavigateToSettingsRequested += () => SwitchTab(TabType.SETTINGS);

            _bgRemovalView.OnStatusChanged += SetStatus;
            _bgRemovalView.OnNavigateToSettingsRequested += () => SwitchTab(TabType.SETTINGS);
        }

        private void UnsubscribeEvents()
        {
            if (_settingsView != null)
            {
                _settingsView.OnStatusChanged -= SetStatus;
                _settingsView.OnSettingsChanged -= OnSettingsChanged;
            }

            if (_chatView != null)
            {
                _chatView.OnStatusChanged -= SetStatus;
            }
            
            if (_imageGenerationView != null)
            {
                _imageGenerationView.OnStatusChanged -= SetStatus;
            }

            if (_bgRemovalView != null)
            {
                _bgRemovalView.OnStatusChanged -= SetStatus;
            }
        }

        private void OnSettingsChanged()
        {
            _chatView?.RefreshProviders();
            _imageGenerationView?.RefreshProviders();
            _bgRemovalView?.RefreshProviders();
        }

        private void SwitchTab(TabType tabType_)
        {
            _settingsTab.EnableInClassList("selected", tabType_ == TabType.SETTINGS);
            _chatTab.EnableInClassList("selected", tabType_ == TabType.CHAT);
            _imageTab.EnableInClassList("selected", tabType_ == TabType.IMAGE_GENERATION);
            _bgRemovalTab.EnableInClassList("selected", tabType_ == TabType.BACKGROUND_REMOVAL);

            _settingsContent.style.display = tabType_ == TabType.SETTINGS ? DisplayStyle.Flex : DisplayStyle.None;
            _settingsContent.style.flexShrink = 1;
            _chatContent.style.display = tabType_ == TabType.CHAT ? DisplayStyle.Flex : DisplayStyle.None;
            _imageContent.style.display = tabType_ == TabType.IMAGE_GENERATION ? DisplayStyle.Flex : DisplayStyle.None;
            _bgRemovalContent.style.display = tabType_ == TabType.BACKGROUND_REMOVAL ? DisplayStyle.Flex : DisplayStyle.None;

            _settingsView?.OnHide();
            _chatView?.OnHide();
            _imageGenerationView?.OnHide();
            _bgRemovalView?.OnHide();

            switch (tabType_)
            {
                case TabType.SETTINGS:
                    _settingsView?.OnShow();
                    break;
                case TabType.CHAT:
                    _chatView?.OnShow();
                    break;
                case TabType.IMAGE_GENERATION:
                    _imageGenerationView?.OnShow();
                    break;
                case TabType.BACKGROUND_REMOVAL:
                    _bgRemovalView?.OnShow();
                    break;
            }

            _storage.SetInt(EditorDataStorageKeys.KEY_MAIN_TAB, (int)tabType_);
            _storage.Save();
        }

        private void SetStatus(string message_)
        {
            if (_statusText != null)
            {
                _statusText.text = message_;
            }
        }
    }
}
