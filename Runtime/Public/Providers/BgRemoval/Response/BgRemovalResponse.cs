using System;
using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a unified background removal response from a provider.
    /// </summary>
    public partial class BgRemovalResponse
    {
        public string Base64Image { get; set; }
        public string Model { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> RawResponse { get; set; }
        public BgRemovalProviderType ProviderType { get; set; } = BgRemovalProviderType.NONE;

        public bool HasImage => !string.IsNullOrEmpty(Base64Image);

        /// <summary>
        /// Creates an error response with the provided message.
        /// </summary>
        /// <param name="errorMessage_">Error message.</param>
        /// <returns>Error response.</returns>
        public static BgRemovalResponse FromError(string errorMessage_)
        {
            return FromErrorInternal(errorMessage_);
        }

        /// <summary>
        /// Gets the image bytes from base64 data.
        /// </summary>
        /// <returns>Decoded image bytes or null.</returns>
        public byte[] GetImageBytes()
        {
            return GetImageBytesInternal();
        }
    }
}
