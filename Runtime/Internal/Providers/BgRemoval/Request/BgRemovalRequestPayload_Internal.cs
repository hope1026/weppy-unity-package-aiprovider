using System;
using System.Collections.Generic;

namespace Weppy.AIProvider
{
    public partial class BgRemovalRequestPayload
    {
        public BgRemovalRequestPayload() { }

        public BgRemovalRequestPayload(string base64Image_)
        {
            Base64Image = base64Image_;
        }

        public BgRemovalRequestPayload(string base64Image_, string mediaType_)
        {
            Base64Image = base64Image_;
            MediaType = mediaType_;
        }

        private BgRemovalRequestPayload WithModelInternal(string model_)
        {
            Model = model_;
            return this;
        }

        private byte[] GetImageBytesInternal()
        {
            if (string.IsNullOrEmpty(Base64Image))
                return null;

            try
            {
                return Convert.FromBase64String(Base64Image);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private BgRemovalRequestPayload WithAdditionalBodyParameterInternal(string key_, object value_)
        {
            if (AdditionalBodyParameters == null)
                AdditionalBodyParameters = new Dictionary<string, object>();
            AdditionalBodyParameters[key_] = value_;
            return this;
        }

        private BgRemovalRequestPayload WithAdditionalBodyParametersInternal(Dictionary<string, object> parameters_)
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
