using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Chat.Editor
{
    public class ProviderEditorWindow : EditorWindow
    {
        private static readonly string EDITOR_PATH = EditorPaths.EDITOR_WINDOW_PATH;

        private enum TabType
        {
            SETTINGS,
            CHAT,
        }

        private EditorDataStorage _storage;
        private SettingsView _settingsView;
        private ChatView _chatView;

        private Button _settingsTab;
        private Button _chatTab;
        private VisualElement _settingsContent;
        private VisualElement _chatContent;
        private Label _statusText;

        [MenuItem("Window/Weppy/AI Provider Chat")]
        public static void ShowWindow()
        {
            ProviderEditorWindow window = GetWindow<ProviderEditorWindow>();
            window.titleContent = new GUIContent("AI Provider Chat");
            window.minSize = new Vector2(500, 700);
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedUI;
            
            _chatView?.SaveHistoryIfNeeded();
            _chatView?.Dispose();
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
            _settingsContent = root_.Q<VisualElement>("settings-content");
            _chatContent = root_.Q<VisualElement>("chat-content");
            _statusText = root_.Q<Label>("status-text");

            UpdateLocalizedUI();
            LocalizationManager.OnLanguageChanged += UpdateLocalizedUI;
        }

        private void UpdateLocalizedUI()
        {
            if (_chatTab != null)
                _chatTab.text = LocalizationManager.Get(LocalizationKeys.MAIN_TAB_CHAT);
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

            _settingsView.OnStatusChanged += SetStatus;
            _settingsView.OnSettingsChanged += OnSettingsChanged;

            _chatView.OnStatusChanged += SetStatus;
            _chatView.OnNavigateToSettingsRequested += () => SwitchTab(TabType.SETTINGS);
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
        }

        private void OnSettingsChanged()
        {
            _chatView?.RefreshProviders();
        }

        private void SwitchTab(TabType tabType_)
        {
            _settingsTab.EnableInClassList("selected", tabType_ == TabType.SETTINGS);
            _chatTab.EnableInClassList("selected", tabType_ == TabType.CHAT);

            _settingsContent.style.display = tabType_ == TabType.SETTINGS ? DisplayStyle.Flex : DisplayStyle.None;
            _settingsContent.style.flexShrink = 1;
            _chatContent.style.display = tabType_ == TabType.CHAT ? DisplayStyle.Flex : DisplayStyle.None;

            _settingsView?.OnHide();
            _chatView?.OnHide();

            switch (tabType_)
            {
                case TabType.SETTINGS:
                    _settingsView?.OnShow();
                    break;
                case TabType.CHAT:
                    _chatView?.OnShow();
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
