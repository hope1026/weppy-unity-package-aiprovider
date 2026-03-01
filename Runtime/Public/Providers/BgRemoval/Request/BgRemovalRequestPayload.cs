using System.Collections.Generic;

namespace Weppy.AIProvider
{
    /// <summary>
    /// Represents a background removal request payload.
    /// </summary>
    public partial class BgRemovalRequestPayload
    {
        public string Base64Image { get; set; }
        public string MediaType { get; set; } = "image/png";
        public string Model { get; set; }
        public Dictionary<string, object> AdditionalBodyParameters { get; set; }

        /// <summary>
        /// Sets the model for this request.
        /// </summary>
        /// <param name="model_">Model ID.</param>
        /// <returns>The updated payload.</returns>
        public BgRemovalRequestPayload WithModel(string model_)
        {
            return WithModelInternal(model_);
        }

        /// <summary>
        /// Gets the image bytes from base64 data.
        /// </summary>
        /// <returns>Decoded image bytes or null.</returns>
        public byte[] GetImageBytes()
        {
            return GetImageBytesInternal();
        }

        /// <summary>
        /// Adds a custom body parameter.
        /// </summary>
        /// <param name="key_">Parameter key.</param>
        /// <param name="value_">Parameter value.</param>
        /// <returns>The updated payload.</returns>
        public BgRemovalRequestPayload WithAdditionalBodyParameter(string key_, object value_)
        {
            return WithAdditionalBodyParameterInternal(key_, value_);
        }

        /// <summary>
        /// Adds custom body parameters.
        /// </summary>
        /// <param name="parameters_">Parameters to merge.</param>
        /// <returns>The updated payload.</returns>
        public BgRemovalRequestPayload WithAdditionalBodyParameters(Dictionary<string, object> parameters_)
        {
            return WithAdditionalBodyParametersInternal(parameters_);
        }
    }
}
