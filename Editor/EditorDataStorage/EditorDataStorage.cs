using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Weppy.AIProvider.Chat.Editor
{
    public class EditorDataStorage
    {
        private const string CHAT_KEY_PREFIX = "AIProviderChat_";
        private const string ENCRYPTED_SUFFIX = "_encrypted";
        private static readonly byte[] DEFAULT_KEY = Encoding.UTF8.GetBytes("AIProviderEditorStorageKey12345!");
        private static readonly byte[] DEFAULT_IV = Encoding.UTF8.GetBytes("AIProviderIV1234");

        private readonly string _keyPrefix;
        private readonly string _allKeysKey;
        private readonly byte[] _encryptionKey;
        private readonly byte[] _encryptionIV;
        private HashSet<string> _allKeys;

        public EditorDataStorage()
        {
            _keyPrefix = CHAT_KEY_PREFIX;
            _allKeysKey = _keyPrefix + "AllKeys";
            _encryptionKey = DEFAULT_KEY;
            _encryptionIV = DEFAULT_IV;
            LoadAllKeys();
        }

        public EditorDataStorage(string keyPrefix_, byte[] encryptionKey_, byte[] encryptionIV_)
        {
            _keyPrefix = keyPrefix_;
            _allKeysKey = keyPrefix_ + "AllKeys";
            _encryptionKey = encryptionKey_ ?? DEFAULT_KEY;
            _encryptionIV = encryptionIV_ ?? DEFAULT_IV;
            LoadAllKeys();
        }

        private void LoadAllKeys()
        {
            _allKeys = new HashSet<string>();
            string keysJson = EditorPrefs.GetString(_allKeysKey, "");
            if (!string.IsNullOrEmpty(keysJson))
            {
                string[] keys = keysJson.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string key in keys)
                {
                    _allKeys.Add(key);
                }
            }
        }

        private void SaveAllKeys()
        {
            string keysJson = string.Join("||", _allKeys);
            EditorPrefs.SetString(_allKeysKey, keysJson);
        }

        private string GetPrefKey(string key_)
        {
            return _keyPrefix + key_;
        }

        private string GetEncryptedFlagKey(string key_)
        {
            return _keyPrefix + key_ + ENCRYPTED_SUFFIX;
        }

        public void SetString(string key_, string value_, bool encrypt_ = false)
        {
            string prefKey = GetPrefKey(key_);
            string encryptedFlagKey = GetEncryptedFlagKey(key_);

            string storedValue = encrypt_ ? Encrypt(value_) : value_;
            EditorPrefs.SetString(prefKey, storedValue);
            EditorPrefs.SetBool(encryptedFlagKey, encrypt_);

            _allKeys.Add(key_);
            SaveAllKeys();
        }

        public string GetString(string key_, string defaultValue_ = "")
        {
            string prefKey = GetPrefKey(key_);
            string encryptedFlagKey = GetEncryptedFlagKey(key_);

            if (!EditorPrefs.HasKey(prefKey))
                return defaultValue_;

            string value = EditorPrefs.GetString(prefKey, defaultValue_);
            bool isEncrypted = EditorPrefs.GetBool(encryptedFlagKey, false);

            return isEncrypted ? Decrypt(value) : value;
        }

        public void SetInt(string key_, int value_)
        {
            string prefKey = GetPrefKey(key_);
            EditorPrefs.SetInt(prefKey, value_);

            _allKeys.Add(key_);
            SaveAllKeys();
        }

        public int GetInt(string key_, int defaultValue_ = 0)
        {
            string prefKey = GetPrefKey(key_);
            return EditorPrefs.GetInt(prefKey, defaultValue_);
        }

        public void SetFloat(string key_, float value_)
        {
            string prefKey = GetPrefKey(key_);
            EditorPrefs.SetFloat(prefKey, value_);

            _allKeys.Add(key_);
            SaveAllKeys();
        }

        public float GetFloat(string key_, float defaultValue_ = 0f)
        {
            string prefKey = GetPrefKey(key_);
            return EditorPrefs.GetFloat(prefKey, defaultValue_);
        }

        public void SetBool(string key_, bool value_)
        {
            string prefKey = GetPrefKey(key_);
            EditorPrefs.SetBool(prefKey, value_);

            _allKeys.Add(key_);
            SaveAllKeys();
        }

        public bool GetBool(string key_, bool defaultValue_ = false)
        {
            string prefKey = GetPrefKey(key_);
            return EditorPrefs.GetBool(prefKey, defaultValue_);
        }

        public bool HasKey(string key_)
        {
            string prefKey = GetPrefKey(key_);
            return EditorPrefs.HasKey(prefKey);
        }

        public void DeleteKey(string key_)
        {
            string prefKey = GetPrefKey(key_);
            string encryptedFlagKey = GetEncryptedFlagKey(key_);

            if (EditorPrefs.HasKey(prefKey))
            {
                EditorPrefs.DeleteKey(prefKey);
            }
            if (EditorPrefs.HasKey(encryptedFlagKey))
            {
                EditorPrefs.DeleteKey(encryptedFlagKey);
            }

            _allKeys.Remove(key_);
            SaveAllKeys();
        }

        public void DeleteAll()
        {
            foreach (string key in _allKeys)
            {
                string prefKey = GetPrefKey(key);
                string encryptedFlagKey = GetEncryptedFlagKey(key);

                if (EditorPrefs.HasKey(prefKey))
                    EditorPrefs.DeleteKey(prefKey);
                if (EditorPrefs.HasKey(encryptedFlagKey))
                    EditorPrefs.DeleteKey(encryptedFlagKey);
            }

            _allKeys.Clear();
            EditorPrefs.DeleteKey(_allKeysKey);
        }

        public void DeleteAllApiKeys()
        {
            List<string> apiKeyKeys = new List<string>();
            foreach (string key in _allKeys)
            {
                if (key.Contains("ApiKey") || key.Contains("API_KEY") || key.Contains("api_key"))
                {
                    apiKeyKeys.Add(key);
                }
            }

            foreach (string key in apiKeyKeys)
            {
                DeleteKey(key);
            }
        }

        public List<string> GetAllKeys()
        {
            return new List<string>(_allKeys);
        }

        public void Save()
        {
            SaveAllKeys();
        }

        public void Load()
        {
            LoadAllKeys();
        }

        private string Encrypt(string plainText_)
        {
            if (string.IsNullOrEmpty(plainText_))
                return plainText_;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.IV = _encryptionIV;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText_);
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EditorDataStorage] Encryption failed: {ex.Message}");
                return plainText_;
            }
        }

        private string Decrypt(string cipherText_)
        {
            if (string.IsNullOrEmpty(cipherText_))
                return cipherText_;

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText_);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.IV = _encryptionIV;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EditorDataStorage] Decryption failed: {ex.Message}");
                return cipherText_;
            }
        }
    }
}
