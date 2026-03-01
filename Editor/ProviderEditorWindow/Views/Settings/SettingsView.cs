using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class SettingsView : VisualElement
    {
        private static readonly string VIEWS_PATH = EditorPaths.VIEWS_PATH;
        private static readonly string UXML_PATH = VIEWS_PATH + "Settings/SettingsView.uxml";
        private static readonly string USS_PATH = VIEWS_PATH + "Settings/SettingsView.uss";

        public event Action<string> OnStatusChanged;
        public event Action OnSettingsChanged;

        private EditorDataStorage _storage;

        private DropdownField _languageField;
        private Button _deleteAllDataButton;
        private string _currentLanguageCode = "en";

        public SettingsView(EditorDataStorage storage_)
        {
            _storage = storage_;
            LoadLayout();
            LoadStyles();
            SetupUI();
            LoadSettings();
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
            StyleSheet themeStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorPaths.THEME_STYLES_PATH);
            if (themeStyles != null)
            {
                styleSheets.Add(themeStyles);
            }

            StyleSheet viewStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(USS_PATH);
            if (viewStyles != null)
            {
                styleSheets.Add(viewStyles);
            }
        }

        private void SetStatus(string message_)
        {
            OnStatusChanged?.Invoke(message_);
        }

        public void OnShow() { }

        public void OnHide() { }

        private void SetupUI()
        {
            LocalizationManager.Initialize();

            _languageField = this.Q<DropdownField>("language-field");
            if (_languageField != null)
            {
                SetupLanguageDropdown();
                _languageField.RegisterValueChangedCallback(OnLanguageChanged);
            }

            _deleteAllDataButton = this.Q<Button>("delete-all-data-button");
            if (_deleteAllDataButton != null)
            {
                _deleteAllDataButton.text = LocalizationManager.Get(LocalizationKeys.SETTINGS_DELETE_ALL_DATA_BUTTON);
                _deleteAllDataButton.clicked += OnDeleteAllDataClicked;
            }
        }

        private void LoadSettings()
        {
            if (_storage == null)
                return;

            _storage.Load();

            _currentLanguageCode = _storage.GetString(EditorDataStorageKeys.KEY_LANGUAGE);
            if (string.IsNullOrEmpty(_currentLanguageCode))
            {
                _currentLanguageCode = LocalizationManager.DEFAULT_LANGUAGE_CODE;
            }

            LocalizationManager.SetLanguage(_currentLanguageCode);
        }

        private void SetupLanguageDropdown()
        {
            if (_languageField == null)
                return;

            List<string> languageCodes = LocalizationManager.GetAvailableLanguageCodes();
            List<string> displayNames = new List<string>();

            foreach (string code in languageCodes)
            {
                displayNames.Add(LocalizationManager.GetLanguageDisplayName(code));
            }

            _languageField.choices = displayNames;

            int currentIndex = languageCodes.IndexOf(_currentLanguageCode);
            if (currentIndex >= 0 && currentIndex < displayNames.Count)
            {
                _languageField.value = displayNames[currentIndex];
            }
            else if (displayNames.Count > 0)
            {
                _languageField.value = displayNames[0];
            }
        }

        private void OnLanguageChanged(ChangeEvent<string> evt_)
        {
            List<string> languageCodes = LocalizationManager.GetAvailableLanguageCodes();
            int selectedIndex = _languageField.choices.IndexOf(evt_.newValue);

            if (selectedIndex >= 0 && selectedIndex < languageCodes.Count)
            {
                string newLanguageCode = languageCodes[selectedIndex];
                _currentLanguageCode = newLanguageCode;

                _storage?.SetString(EditorDataStorageKeys.KEY_LANGUAGE, _currentLanguageCode);
                _storage?.Save();

                LocalizationManager.SetLanguage(_currentLanguageCode);

                SetStatus(LocalizationManager.Get(LocalizationKeys.SETTINGS_LANGUAGE_CHANGED));
                OnSettingsChanged?.Invoke();
            }
        }

        private void OnDeleteAllDataClicked()
        {
            string title = LocalizationManager.Get(LocalizationKeys.SETTINGS_DELETE_ALL_DATA_TITLE);
            string message = LocalizationManager.Get(LocalizationKeys.SETTINGS_DELETE_ALL_DATA_MESSAGE);

            bool confirmed = EditorUtility.DisplayDialog(
                title,
                message,
                LocalizationManager.Get(LocalizationKeys.COMMON_DELETE),
                LocalizationManager.Get(LocalizationKeys.COMMON_CANCEL)
            );

            if (confirmed)
            {
                _storage?.DeleteAll();
                SetStatus(LocalizationManager.Get(LocalizationKeys.SETTINGS_DELETE_ALL_DATA_SUCCESS));
                OnSettingsChanged?.Invoke();
            }
        }
    }
}
