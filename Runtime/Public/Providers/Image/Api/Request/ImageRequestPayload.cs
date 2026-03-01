using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents an image generation request payload.
    /// </summary>
    public partial class ImageRequestPayload
    {
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public string Model { get; set; }
        public int NumberOfImages { get; set; } = 1;
        public List<ImageRequestInputData> InputImages { get; set; }
        public Dictionary<string, object> AdditionalBodyParameters { get; set; }

        public OpenAIImageRequestOptions OpenAIRequestOptions { get; set; }
        public GoogleGeminiImageRequestOptions GoogleGeminiOptions { get; set; }
        public GoogleImagenImageRequestOptions GoogleImagenOptions { get; set; }
        public OpenRouterImageRequestOptions OpenRouterRequestOptions { get; set; }

        /// <summary>
        /// Sets the negative prompt for the request.
        /// </summary>
        /// <param name="negativePrompt_">Negative prompt text.</param>
        /// <returns>The updated payload.</returns>
        public ImageRequestPayload WithNegativePrompt(string negativePrompt_)
        {
            return WithNegativePromptInternal(negativePrompt_);
        }

        /// <summary>
        /// Sets the number of images to generate.
        /// </summary>
        /// <param name="numberOfImages_">Number of images.</param>
        /// <returns>The updated payload.</returns>
        public ImageRequestPayload WithNumberOfImages(int numberOfImages_)
        {
            return WithNumberOfImagesInternal(numberOfImages_);
        }

        /// <summary>
        /// Adds an input image to the request.
        /// </summary>
        /// <param name="base64Data_">Base64 image data.</param>
        /// <param name="mediaType_">Image media type.</param>
        /// <returns>The updated payload.</returns>
        public ImageRequestPayload WithInputImage(string base64Data_, string mediaType_)
        {
            return WithInputImageInternal(base64Data_, mediaType_);
        }

        /// <summary>
        /// Adds input images to the request.
        /// </summary>
        /// <param name="images_">Input images list.</param>
        /// <returns>The updated payload.</returns>
        public ImageRequestPayload WithInputImages(List<ImageRequestInputData> images_)
        {
            return WithInputImagesInternal(images_);
        }

        /// <summary>
        /// Adds a custom body parameter.
        /// </summary>
        /// <param name="key_">Parameter key.</param>
        /// <param name="value_">Parameter value.</param>
        /// <returns>The updated payload.</returns>
        public ImageRequestPayload WithAdditionalBodyParameter(string key_, object value_)
        {
            return WithAdditionalBodyParameterInternal(key_, value_);
        }

        /// <summary>
        /// Adds custom body parameters.
        /// </summary>
        /// <param name="parameters_">Parameters to merge.</param>
        /// <returns>The updated payload.</returns>
        public ImageRequestPayload WithAdditionalBodyParameters(Dictionary<string, object> parameters_)
        {
            return WithAdditionalBodyParametersInternal(parameters_);
        }
    }
}
