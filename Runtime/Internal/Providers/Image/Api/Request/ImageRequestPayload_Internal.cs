using System.Collections.Generic;

namespace Weppy.AIProvider
{
    public partial class ImageRequestPayload
    {

        public ImageRequestPayload(string prompt_, string model_ = null)
        {
            Prompt = prompt_;
            Model = model_;
        }

        private ImageRequestPayload WithNegativePromptInternal(string negativePrompt_)
        {
            NegativePrompt = negativePrompt_;
            return this;
        }

        private ImageRequestPayload WithNumberOfImagesInternal(int numberOfImages_)
        {
            NumberOfImages = numberOfImages_;
            return this;
        }

        private ImageRequestPayload WithInputImageInternal(string base64Data_, string mediaType_)
        {
            if (InputImages == null)
                InputImages = new List<ImageRequestInputData>();
            InputImages.Add(new ImageRequestInputData(base64Data_, mediaType_));
            return this;
        }

        private ImageRequestPayload WithInputImagesInternal(List<ImageRequestInputData> images_)
        {
            InputImages = images_;
            return this;
        }

        private ImageRequestPayload WithAdditionalBodyParameterInternal(string key_, object value_)
        {
            if (AdditionalBodyParameters == null)
                AdditionalBodyParameters = new Dictionary<string, object>();
            AdditionalBodyParameters[key_] = value_;
            return this;
        }

        private ImageRequestPayload WithAdditionalBodyParametersInternal(Dictionary<string, object> parameters_)
        {
            if (parameters_ == null)
                return this;

            if (AdditionalBodyParameters == null)
                AdditionalBodyParameters = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> kvp in parameters_)
            {
                AdditionalBodyParameters[kvp.Key] = kvp.Value;
            }
            return this;
        }
    }
}
