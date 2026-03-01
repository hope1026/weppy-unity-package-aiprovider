namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents an input image for image generation requests.
    /// </summary>
    public class ImageRequestInputData
    {
        public string Base64Data { get; set; }
        public string MediaType { get; set; }

        /// <summary>
        /// Creates input image data with base64 content and media type.
        /// </summary>
        /// <param name="base64Data_">Base64 image data.</param>
        /// <param name="mediaType_">Image media type.</param>
        public ImageRequestInputData(string base64Data_, string mediaType_)
        {
            Base64Data = base64Data_;
            MediaType = mediaType_;
        }
    }
}
