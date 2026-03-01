using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Weppy.AIProvider.Editor
{
    public static class EditorHistoryStorage
    {
        private const string HISTORY_ROOT_FOLDER = "Library/AIProviderHistory";
        private const string ENTRY_FILE_NAME = "entry.json";

        public static string CreateEntryId()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        }

        public static List<HistoryEntryInfo> ListEntries(HistoryFeatureType featureType_)
        {
            List<HistoryEntryInfo> entries = new List<HistoryEntryInfo>();
            string featurePath = GetFeatureFolderPath(featureType_);
            if (!Directory.Exists(featurePath))
                return entries;

            string[] entryDirs = Directory.GetDirectories(featurePath);
            Array.Sort(entryDirs, StringComparer.Ordinal);
            Array.Reverse(entryDirs);

            foreach (string entryDir in entryDirs)
            {
                string entryFile = Path.Combine(entryDir, ENTRY_FILE_NAME);
                if (!File.Exists(entryFile))
                    continue;

                try
                {
                    string json = File.ReadAllText(entryFile);
                    HistoryEntryInfo info = JsonUtility.FromJson<HistoryEntryInfo>(json);
                    if (info != null && !string.IsNullOrEmpty(info.Id))
                    {
                        entries.Add(info);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AIProvider] Failed to read history entry: {ex.Message}");
                }
            }

            return entries;
        }

        public static ChatHistorySnapshot LoadChatHistory(string entryId_)
        {
            return LoadSnapshot<ChatHistorySnapshot>(HistoryFeatureType.CHAT, entryId_);
        }

        public static ImageHistorySnapshot LoadImageHistory(string entryId_)
        {
            return LoadSnapshot<ImageHistorySnapshot>(HistoryFeatureType.IMAGE, entryId_);
        }

        public static BgRemovalHistorySnapshot LoadBgRemovalHistory(string entryId_)
        {
            return LoadSnapshot<BgRemovalHistorySnapshot>(HistoryFeatureType.BG_REMOVAL, entryId_);
        }

        public static bool SaveChatHistory(ChatHistorySnapshot snapshot_)
        {
            return SaveSnapshot(HistoryFeatureType.CHAT, snapshot_, null);
        }

        public static bool SaveImageHistory(ImageHistorySnapshot snapshot_, List<HistoryTextureData> textures_)
        {
            return SaveSnapshot(HistoryFeatureType.IMAGE, snapshot_, textures_);
        }

        public static bool SaveBgRemovalHistory(BgRemovalHistorySnapshot snapshot_, List<HistoryTextureData> textures_)
        {
            return SaveSnapshot(HistoryFeatureType.BG_REMOVAL, snapshot_, textures_);
        }

        public static Texture2D LoadTexture(HistoryFeatureType featureType_, string entryId_, string fileName_)
        {
            if (string.IsNullOrEmpty(fileName_))
                return null;

            string entryPath = GetEntryFolderPath(featureType_, entryId_);
            string filePath = Path.Combine(entryPath, fileName_);
            if (!File.Exists(filePath))
                return null;

            try
            {
                byte[] imageBytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes))
                    return texture;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIProvider] Failed to load history texture: {ex.Message}");
            }

            return null;
        }

        private static string GetProjectRootPath()
        {
            string assetsPath = Application.dataPath;
            DirectoryInfo parent = Directory.GetParent(assetsPath);
            return parent != null ? parent.FullName : assetsPath;
        }

        private static string GetHistoryRootPath()
        {
            string projectRoot = GetProjectRootPath();
            return Path.Combine(projectRoot, HISTORY_ROOT_FOLDER);
        }

        private static string GetFeatureFolderPath(HistoryFeatureType featureType_)
        {
            string root = GetHistoryRootPath();
            string featureFolder = GetFeatureFolderName(featureType_);
            return Path.Combine(root, featureFolder);
        }

        private static string GetEntryFolderPath(HistoryFeatureType featureType_, string entryId_)
        {
            string featurePath = GetFeatureFolderPath(featureType_);
            return Path.Combine(featurePath, entryId_);
        }

        private static string GetFeatureFolderName(HistoryFeatureType featureType_)
        {
            switch (featureType_)
            {
                case HistoryFeatureType.CHAT:
                    return "Chat";
                case HistoryFeatureType.IMAGE:
                    return "Image";
                case HistoryFeatureType.BG_REMOVAL:
                    return "BgRemoval";
                default:
                    return "Unknown";
            }
        }

        private static bool SaveSnapshot<TSnapshot>(HistoryFeatureType featureType_, TSnapshot snapshot_, List<HistoryTextureData> textures_)
            where TSnapshot : class
        {
            if (snapshot_ == null)
                return false;

            try
            {
                string entryId = GetSnapshotId(snapshot_);
                if (string.IsNullOrEmpty(entryId))
                    return false;

                string entryPath = GetEntryFolderPath(featureType_, entryId);
                Directory.CreateDirectory(entryPath);

                if (textures_ != null)
                {
                    foreach (HistoryTextureData textureData in textures_)
                    {
                        if (textureData == null || textureData.Texture == null || string.IsNullOrEmpty(textureData.FileName))
                            continue;

                        string filePath = Path.Combine(entryPath, textureData.FileName);
                        byte[] pngData = textureData.Texture.EncodeToPNG();
                        File.WriteAllBytes(filePath, pngData);
                    }
                }

                string json = JsonUtility.ToJson(snapshot_, true);
                string entryFile = Path.Combine(entryPath, ENTRY_FILE_NAME);
                File.WriteAllText(entryFile, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIProvider] Failed to save history snapshot: {ex.Message}");
                return false;
            }
        }

        private static string GetSnapshotId(object snapshot_)
        {
            if (snapshot_ is ChatHistorySnapshot chatSnapshot)
                return chatSnapshot.Id;
            if (snapshot_ is ImageHistorySnapshot imageSnapshot)
                return imageSnapshot.Id;
            if (snapshot_ is BgRemovalHistorySnapshot bgSnapshot)
                return bgSnapshot.Id;
            return null;
        }

        private static TSnapshot LoadSnapshot<TSnapshot>(HistoryFeatureType featureType_, string entryId_)
            where TSnapshot : class
        {
            if (string.IsNullOrEmpty(entryId_))
                return null;

            string entryPath = GetEntryFolderPath(featureType_, entryId_);
            string entryFile = Path.Combine(entryPath, ENTRY_FILE_NAME);
            if (!File.Exists(entryFile))
                return null;

            try
            {
                string json = File.ReadAllText(entryFile);
                return JsonUtility.FromJson<TSnapshot>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AIProvider] Failed to load history snapshot: {ex.Message}");
                return null;
            }
        }
    }
}
