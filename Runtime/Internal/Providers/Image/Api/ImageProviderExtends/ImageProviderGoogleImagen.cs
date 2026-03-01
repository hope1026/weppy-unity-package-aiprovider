using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    internal class ImageProviderGoogleImagen : ImageProviderAbstract
    {
        internal override ImageProviderType ProviderType => ImageProviderType.GOOGLE_IMAGEN;

        internal ImageProviderGoogleImagen(ImageProviderSettings settings_) : base(settings_, GoogleImageApiConfig.BASE_URL)
        {
        }

        internal override async Task<ImageResponse> GenerateImageAsync(
            ImageRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return ImageResponse.FromError("Request cannot be null.");

            try
            {
                string model = model_ ?? _settings.DefaultModel ?? GoogleImageApiConfig.DEFAULT_IMAGEN_MODEL;
                Dictionary<string, object> body = BuildRequestBody(requestPayload_);
                string url = GoogleImageApiConfig.GetImagenUrl(_settings.BaseUrl, model, _settings.ApiKey);
                Dictionary<string, string> headers = GoogleImageApiConfig.GetHeaders(_settings.CustomHeaders);

                HttpResponseResult response = await _httpClient.PostJsonAsync(url, body, headers, cancellationToken_);

                if (!response.IsSuccess)
                {
                    string errorMessage = response.ErrorMessage ?? $"HTTP {response.StatusCode}: {response.Content}";
                    LogRequestError(model, url, body, errorMessage);
                    return ImageResponse.FromError(errorMessage);
                }

                return ParseResponse(response.GetJsonContent(), model);
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel ?? GoogleImageApiConfig.DEFAULT_IMAGEN_MODEL, null, null, ex.Message);
                return ImageResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string model_, string url_, Dictionary<string, object> body_, string errorMessage_)
        {
            string bodyJson = GetSanitizedBodyJson(body_);
            AIProviderLogger.LogError($"[GoogleImagen Image] Request failed - Model: {model_}, URL: {url_ ?? "(unknown)"}, Body: {bodyJson}, Error: {errorMessage_}");
        }

        private string GetSanitizedBodyJson(Dictionary<string, object> body_)
        {
            if (body_ == null)
                return "(null)";

            try
            {
                Dictionary<string, object> sanitized = SanitizeBodyForLogging(body_);
                string json = JsonHelper.Serialize(sanitized);
                if (json != null && json.Length > 500)
                    return json.Substring(0, 500) + "...(truncated)";
                return json ?? "(serialize failed)";
            }
            catch
            {
                return "(serialize error)";
            }
        }

        private Dictionary<string, object> SanitizeBodyForLogging(Dictionary<string, object> body_)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> kvp in body_)
            {
                result[kvp.Key] = SanitizeValue(kvp.Value);
            }
            return result;
        }

        private object SanitizeValue(object value_)
        {
            if (value_ == null)
                return null;

            if (value_ is string strValue)
            {
                if (strValue.Length > 100)
                    return strValue.Substring(0, 100) + $"...(length:{strValue.Length})";
                return strValue;
            }

            if (value_ is Dictionary<string, object> dict)
            {
                if (dict.ContainsKey("data") || dict.ContainsKey("inlineData"))
                    return "(base64 image data)";
                return SanitizeBodyForLogging(dict);
            }

            if (value_ is object[] arr)
            {
                object[] sanitizedArr = new object[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                    sanitizedArr[i] = SanitizeValue(arr[i]);
                return sanitizedArr;
            }

            if (value_ is List<object> list)
            {
                List<object> sanitizedList = new List<object>();
                foreach (object item in list)
                    sanitizedList.Add(SanitizeValue(item));
                return sanitizedList;
            }

            return value_;
        }

        private Dictionary<string, object> BuildRequestBody(ImageRequestPayload requestPayload_)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                ["sampleCount"] = requestPayload_.NumberOfImages
            };

            GoogleImagenImageRequestOptions options = requestPayload_.GoogleImagenOptions;
            if (options != null)
            {
                if (!string.IsNullOrEmpty(options.AspectRatio))
                    parameters["aspectRatio"] = options.AspectRatio;
            }

            // Handle AdditionalBodyParameters (from dynamic options in Editor)
            if (requestPayload_.AdditionalBodyParameters != null)
            {
                foreach (KeyValuePair<string, object> kvp in requestPayload_.AdditionalBodyParameters)
                {
                    if (kvp.Key == "aspectRatio" && !string.IsNullOrEmpty(kvp.Value?.ToString()))
                    {
                        parameters["aspectRatio"] = kvp.Value.ToString();
                    }
                    // Ignore other parameters (e.g., size, quality, style from OpenAI)
                }
            }

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["instances"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["prompt"] = requestPayload_.Prompt
                    }
                },
                ["parameters"] = parameters
            };

            return body;
        }

        private ImageResponse ParseResponse(Dictionary<string, object> json_, string model_)
        {
            if (json_ == null)
                return ImageResponse.FromError("Invalid response format");

            ImageResponse response = new ImageResponse
            {
                Model = model_,
                RawResponse = json_
            };

            List<object> predictions = JsonHelper.GetArray(json_, "predictions");
            if (predictions != null)
            {
                for (int i = 0; i < predictions.Count; i++)
                {
                    Dictionary<string, object> prediction = predictions[i] as Dictionary<string, object>;
                    if (prediction == null)
                        continue;

                    string bytesBase64 = JsonHelper.GetValue<string>(prediction, "bytesBase64Encoded");
                    if (string.IsNullOrEmpty(bytesBase64))
                        continue;

                    ImageResponseGeneratedImage image = new ImageResponseGeneratedImage
                    {
                        Index = i,
                        Base64Data = bytesBase64
                    };

                    response.Images.Add(image);
                }
            }

            return response;
        }
    }
}
