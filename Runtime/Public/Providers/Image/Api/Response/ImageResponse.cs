using System;
using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a unified image generation response from a provider.
    /// </summary>
    public partial class ImageResponse
    {
        public List<ImageResponseGeneratedImage> Images { get; set; } = new List<ImageResponseGeneratedImage>();
        public string Model { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; }
        public string Content { get; set; }
        public Dictionary<string, object> RawResponse { get; set; }
        public ImageProviderType ProviderType { get; set; } = ImageProviderType.NONE;

        public ImageResponseGeneratedImage FirstImage => Images.Count > 0 ? Images[0] : null;

        /// <summary>
        /// Creates an error response with the provided message.
        /// </summary>
        /// <param name="errorMessage_">Error message.</param>
        /// <returns>Error response.</returns>
        public static ImageResponse FromError(string errorMessage_)
        {
            return FromErrorInternal(errorMessage_);
        }
    }
}
