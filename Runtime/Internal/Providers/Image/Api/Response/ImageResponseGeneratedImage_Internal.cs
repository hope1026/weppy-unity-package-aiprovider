using System;
using UnityEngine;

namespace Weppy.AIProvider
{
    public partial class ImageResponseGeneratedImage
    {
        private Texture2D CreateTexture2DInternal()
        {
            if (string.IsNullOrEmpty(Base64Data))
                return null;

            try
            {
                byte[] imageBytes = GetImageBytes();
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageBytes))
                {
                    return texture;
                }
            }
            catch (Exception ex)
            {
                AIProviderLogger.LogError($"[AIProvider] Failed to create texture: {ex.Message}");
            }

            return null;
        }
    }
}