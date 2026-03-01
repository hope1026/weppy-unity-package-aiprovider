using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    internal class ImageProviderOpenAI : ImageProviderAbstract
    {
        internal override ImageProviderType ProviderType => ImageProviderType.OPEN_AI;

        internal ImageProviderOpenAI(ImageProviderSettings settings_) : base(settings_, OpenAIImageApiConfig.BASE_URL)
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
                Dictionary<string, object> body = BuildRequestBody(requestPayload_, model_);
                string url = $"{_settings.BaseUrl}{OpenAIImageApiConfig.IMAGE_ENDPOINT}";
                Dictionary<string, string> headers = OpenAIImageApiConfig.GetAuthHeaders(
                    _settings.ApiKey, _settings.OrganizationId, _settings.CustomHeaders);

                HttpResponseResult response = await _httpClient.PostJsonAsync(url, body, headers, cancellationToken_);

                if (!response.IsSuccess)
                {
                    string errorMessage = response.ErrorMessage ?? $"HTTP {response.StatusCode}: {response.Content}";
                    LogRequestError(model_ ?? _settings.DefaultModel ?? OpenAIImageApiConfig.DEFAULT_IMAGE_MODEL, url, body, errorMessage);
                    return ImageResponse.FromError(errorMessage);
                }

                return ParseResponse(response.GetJsonContent(), model_ ?? _settings.DefaultModel ?? OpenAIImageApiConfig.DEFAULT_IMAGE_MODEL);
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel ?? OpenAIImageApiConfig.DEFAULT_IMAGE_MODEL, null, null, ex.Message);
                return ImageResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string model_, string url_, Dictionary<string, object> body_, string errorMessage_)
        {
            string bodyJson = GetSanitizedBodyJson(body_);
            AIProviderLogger.LogError($"[OpenAI Image] Request failed - Model: {model_}, URL: {url_ ?? "(unknown)"}, Body: {bodyJson}, Error: {errorMessage_}");
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
                if (dict.ContainsKey("data") || dict.ContainsKey("inlineData") || dict.ContainsKey("b64_json"))
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

        private Dictionary<string, object> BuildRequestBody(ImageRequestPayload requestPayload_, string model_)
        {
            string model = model_ ?? _settings.DefaultModel ?? OpenAIImageApiConfig.DEFAULT_IMAGE_MODEL;

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["model"] = model,
                ["prompt"] = requestPayload_.Prompt,
                ["n"] = requestPayload_.NumberOfImages
            };

            // gpt-image-1 does not support response_format parameter
            bool isGptImageModel = model.Contains("gpt-image");
            if (!isGptImageModel)
            {
                body["response_format"] = "b64_json";
            }

            OpenAIImageRequestOptions requestOptions = requestPayload_.OpenAIRequestOptions;
            if (requestOptions != null)
            {
                if (!string.IsNullOrEmpty(requestOptions.ResponseFormat) && !isGptImageModel)
                    body["response_format"] = requestOptions.ResponseFormat;

                if (!string.IsNullOrEmpty(requestOptions.Size))
                    body["size"] = requestOptions.Size;

                if (model.Contains("dall-e-3"))
                {
                    if (!string.IsNullOrEmpty(requestOptions.Quality))
                        body["quality"] = requestOptions.Quality;

                    if (!string.IsNullOrEmpty(requestOptions.Style))
                        body["style"] = requestOptions.Style;
                }
            }

            if (requestPayload_.AdditionalBodyParameters != null)
            {
                foreach (KeyValuePair<string, object> kvp in requestPayload_.AdditionalBodyParameters)
                {
                    body[kvp.Key] = kvp.Value;
                }
            }

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

            List<object> data = JsonHelper.GetArray(json_, "data");
            if (data != null)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    Dictionary<string, object> item = data[i] as Dictionary<string, object>;
                    if (item == null)
                        continue;

                    ImageResponseGeneratedImage image = new ImageResponseGeneratedImage
                    {
                        Index = i,
                        Base64Data = JsonHelper.GetValue<string>(item, "b64_json"),
                        Url = JsonHelper.GetValue<string>(item, "url"),
                        RevisedPrompt = JsonHelper.GetValue<string>(item, "revised_prompt")
                    };

                    response.Images.Add(image);
                }
            }

            return response;
        }
    }
}
