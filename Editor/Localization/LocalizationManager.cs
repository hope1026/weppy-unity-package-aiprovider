using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Weppy.AIProvider.Chat.Editor
{
    public static class LocalizationManager
    {
        public const string DEFAULT_LANGUAGE_CODE = "en";
        private static Dictionary<string, string> _currentStrings = new();
        private static Dictionary<string, LanguageInfo> _availableLanguages = new();
        private static string _currentLanguageCode = DEFAULT_LANGUAGE_CODE;
        private static EditorDataStorage _storage;
        public static event Action OnLanguageChanged;

        public static string CurrentLanguageCode => _currentLanguageCode;
        public static IReadOnlyDictionary<string, LanguageInfo> AvailableLanguages => _availableLanguages;


        static LocalizationManager()
        {
            _storage = new EditorDataStorage();
            LoadAvailableLanguages();

            string initialLanguage = GetInitialLanguage();
            LoadLanguage(initialLanguage);
        }

        private static string GetInitialLanguage()
        {
            string savedLanguage = _storage.GetString(EditorDataStorageKeys.KEY_LANGUAGE);
            if (!string.IsNullOrEmpty(savedLanguage) && _availableLanguages.ContainsKey(savedLanguage))
            {
                return savedLanguage;
            }

            string systemLanguage = GetSystemLanguageCode();
            if (_availableLanguages.ContainsKey(systemLanguage))
            {
                return systemLanguage;
            }

            return DEFAULT_LANGUAGE_CODE;
        }

        private static string GetSystemLanguageCode()
        {
            SystemLanguage systemLanguage = Application.systemLanguage;
            return systemLanguage switch
            {
                SystemLanguage.Korean => "ko",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.Chinese => "zh",
                SystemLanguage.ChineseSimplified => "zh",
                SystemLanguage.ChineseTraditional => "zh-TW",
                SystemLanguage.French => "fr",
                SystemLanguage.German => "de",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Italian => "it",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Russian => "ru",
                SystemLanguage.Arabic => "ar",
                SystemLanguage.Dutch => "nl",
                SystemLanguage.Polish => "pl",
                SystemLanguage.Turkish => "tr",
                SystemLanguage.Vietnamese => "vi",
                SystemLanguage.Thai => "th",
                SystemLanguage.Indonesian => "id",
                _ => DEFAULT_LANGUAGE_CODE
            };
        }

        public static void Initialize()
        {
            LoadAvailableLanguages();
        }

        private static void LoadLanguage(string languageCode_)
        {
            _currentStrings.Clear();

            if (!_availableLanguages.TryGetValue(languageCode_, out LanguageInfo languageInfo))
            {
                if (languageCode_ != DEFAULT_LANGUAGE_CODE && _availableLanguages.ContainsKey(DEFAULT_LANGUAGE_CODE))
                {
                    languageInfo = _availableLanguages[DEFAULT_LANGUAGE_CODE];
                    languageCode_ = DEFAULT_LANGUAGE_CODE;
                }
                else
                {
                    Debug.LogWarning($"[LocalizationManager] Language not found: {languageCode_}");
                    _currentLanguageCode = languageCode_;
                    return;
                }
            }

            try
            {
                string json = File.ReadAllText(languageInfo.FilePath);
                LocalizationData data = JsonUtility.FromJson<LocalizationData>(json);

                if (data?.strings != null)
                {
                    foreach (LocalizationEntry entry in data.strings)
                    {
                        if (!string.IsNullOrEmpty(entry.key))
                        {
                            _currentStrings[entry.key] = entry.value ?? "";
                        }
                    }
                }

                _currentLanguageCode = languageCode_;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LocalizationManager] Failed to load language: {languageCode_}\n{exception.Message}");
            }
        }

        public static void LoadAvailableLanguages()
        {
            _availableLanguages.Clear();

            string fullPath = Path.GetFullPath(EditorPaths.LOCALIZATION_PATH);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[LocalizationManager] Languages directory not found: {fullPath}");
                return;
            }

            string[] jsonFiles = Directory.GetFiles(fullPath, "*.json");
            foreach (string filePath in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    LocalizationData data = JsonUtility.FromJson<LocalizationData>(json);

                    if (data != null && !string.IsNullOrEmpty(data.languageCode))
                    {
                        LanguageInfo info = new LanguageInfo
                        {
                            DisplayName = data.languageDisplayName,
                            FilePath = filePath
                        };
                        _availableLanguages[data.languageCode] = info;
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[LocalizationManager] Failed to load language file: {filePath}\n{exception.Message}");
                }
            }
        }

        public static void SetLanguage(string languageCode_)
        {
            if (string.IsNullOrEmpty(languageCode_))
                languageCode_ = DEFAULT_LANGUAGE_CODE;

            if (_currentLanguageCode == languageCode_ && _currentStrings.Count > 0)
                return;

            LoadLanguage(languageCode_);
            _storage.SetString(EditorDataStorageKeys.KEY_LANGUAGE, _currentLanguageCode);
            OnLanguageChanged?.Invoke();
        }

        public static string Get(string key_)
        {
            if (string.IsNullOrEmpty(key_))
                return "";

            if (_currentStrings.TryGetValue(key_, out string value))
                return value;

            return key_;
        }

        public static string Get(string key_, params object[] args_)
        {
            string template = Get(key_);
            try
            {
                return string.Format(template, args_);
            }
            catch
            {
                return template;
            }
        }

        public static List<string> GetAvailableLanguageCodes()
        {
            return new List<string>(_availableLanguages.Keys);
        }

        public static string GetLanguageDisplayName(string languageCode_)
        {
            if (_availableLanguages.TryGetValue(languageCode_, out LanguageInfo info))
                return info.DisplayName;
            return languageCode_;
        }
    }
}