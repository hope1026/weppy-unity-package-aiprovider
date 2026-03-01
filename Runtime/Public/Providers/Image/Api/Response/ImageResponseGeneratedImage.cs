using System;
using UnityEngine;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a generated image item in a response.
    /// </summary>
    public partial class ImageResponseGeneratedImage
    {
        public string Base64Data { get; set; }
        public string Url { get; set; }
        public string RevisedPrompt { get; set; }
        public int Index { get; set; }

        /// <summary>
        /// Gets the image bytes from base64 data.
        /// </summary>
        /// <returns>Decoded image bytes or null.</returns>
        public byte[] GetImageBytes()
        {
            if (string.IsNullOrEmpty(Base64Data))
                return null;

            return Convert.FromBase64String(Base64Data);
        }

        public bool HasData => !string.IsNullOrEmpty(Base64Data) || !string.IsNullOrEmpty(Url);

        /// <summary>
        /// Creates a Texture2D from the Base64Data.
        /// </summary>
        /// <returns>Texture2D instance if successful, null otherwise</returns>
        public Texture2D CreateTexture2D()
        {
            return CreateTexture2DInternal();
        }
    }
}
