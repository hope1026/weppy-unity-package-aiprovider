using System;
using UnityEngine;
using System.IO;

namespace Weppy.AIProvider.Editor
{
    public static class ImageHelper
    {
        private static readonly string[] SUPPORTED_EXTENSIONS = { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

        public static string GetMediaType(string extension_)
        {
            if (string.IsNullOrEmpty(extension_))
                return "image/png";

            string ext = extension_.ToLowerInvariant();
            if (!ext.StartsWith("."))
                ext = "." + ext;

            return ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "image/png"
            };
        }

        public static string GetExtensionFromMediaType(string mediaType_)
        {
            if (string.IsNullOrEmpty(mediaType_))
                return ".png";

            return mediaType_.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".png"
            };
        }

        public static bool IsSupportedImageExtension(string extension_)
        {
            if (string.IsNullOrEmpty(extension_))
                return false;

            string ext = extension_.ToLowerInvariant();
            if (!ext.StartsWith("."))
                ext = "." + ext;

            foreach (string supportedExt in SUPPORTED_EXTENSIONS)
            {
                if (ext == supportedExt)
                    return true;
            }

            return false;
        }

        public static bool IsImageFilePath(string path_)
        {
            if (string.IsNullOrEmpty(path_))
                return false;

            if (!File.Exists(path_))
                return false;

            string extension = Path.GetExtension(path_);
            return IsSupportedImageExtension(extension);
        }

        public static Texture2D CreateThumbnail(Texture2D source_, int width_, int height_)
        {
            if (source_ == null)
            {
                Debug.LogError("[AIProvider] ImageHelper.CreateThumbnail: source texture is null");
                return null;
            }

            RenderTexture rt = RenderTexture.GetTemporary(width_, height_, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;
            Graphics.Blit(source_, rt);

            Texture2D thumbnail = new Texture2D(width_, height_, TextureFormat.RGBA32, false);
            thumbnail.ReadPixels(new Rect(0, 0, width_, height_), 0, 0);
            thumbnail.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return thumbnail;
        }

        public static Texture2D LoadTextureFromFile(string filePath_)
        {
            if (string.IsNullOrEmpty(filePath_))
            {
                Debug.LogError("[AIProvider] ImageHelper.LoadTextureFromFile: filePath is null or empty");
                return null;
            }

            if (!File.Exists(filePath_))
            {
                Debug.LogError($"[AIProvider] ImageHelper.LoadTextureFromFile: Image file not found - {filePath_}");
                return null;
            }

            byte[] imageBytes = File.ReadAllBytes(filePath_);
            return LoadTextureFromBytes(imageBytes);
        }

        public static Texture2D LoadTextureFromBytes(byte[] imageBytes_)
        {
            if (imageBytes_ == null || imageBytes_.Length == 0)
            {
                Debug.LogError("[AIProvider] ImageHelper.LoadTextureFromBytes: imageBytes is null or empty");
                return null;
            }

            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(imageBytes_))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                Debug.LogError("[AIProvider] ImageHelper.LoadTextureFromBytes: Failed to load image from bytes");
                return null;
            }

            return texture;
        }

        public static Texture2D LoadTextureFromBase64(string base64Data_)
        {
            if (string.IsNullOrEmpty(base64Data_))
            {
                Debug.LogError("[AIProvider] ImageHelper.LoadTextureFromBase64: base64Data is null or empty");
                return null;
            }

            byte[] imageBytes = Convert.FromBase64String(base64Data_);
            return LoadTextureFromBytes(imageBytes);
        }

        public static string EncodeToBase64(Texture2D texture_, bool usePng_ = true)
        {
            if (texture_ == null)
            {
                Debug.LogError("[AIProvider] ImageHelper.EncodeToBase64: texture is null");
                return null;
            }

            byte[] bytes = usePng_ ? texture_.EncodeToPNG() : texture_.EncodeToJPG();
            return Convert.ToBase64String(bytes);
        }

        public static byte[] EncodeToBytes(Texture2D texture_, bool usePng_ = true)
        {
            if (texture_ == null)
            {
                Debug.LogError("[AIProvider] ImageHelper.EncodeToBytes: texture is null");
                return null;
            }

            return usePng_ ? texture_.EncodeToPNG() : texture_.EncodeToJPG();
        }
    }
}
