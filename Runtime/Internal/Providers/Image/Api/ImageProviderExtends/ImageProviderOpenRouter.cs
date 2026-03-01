using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    internal class ImageProviderOpenRouter : ImageProviderAbstract
    {
        internal override ImageProviderType ProviderType => ImageProviderType.OPEN_ROUTER;

        internal ImageProviderOpenRouter(ImageProviderSettings settings_) : base(settings_, OpenRouterImageApiConfig.BASE_URL)
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
                string url = $"{_settings.BaseUrl}{OpenRouterImageApiConfig.IMAGE_ENDPOINT}";
                Dictionary<string, string> headers = GetHeaders(requestPayload_);

                HttpResponseResult response = await _httpClient.PostJsonAsync(url, body, headers, cancellationToken_);

                if (!response.IsSuccess)
                {
                    string errorMessage = response.ErrorMessage ?? $"HTTP {response.StatusCode}: {response.Content}";
                    LogRequestError(model_ ?? _settings.DefaultModel ?? OpenRouterImageApiConfig.DEFAULT_IMAGE_MODEL, url, body, errorMessage);
                    return ImageResponse.FromError(errorMessage);
                }

                return ParseResponse(response.GetJsonContent(), model_ ?? _settings.DefaultModel ?? OpenRouterImageApiConfig.DEFAULT_IMAGE_MODEL);
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel ?? OpenRouterImageApiConfig.DEFAULT_IMAGE_MODEL, null, null, ex.Message);
                return ImageResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string model_, string url_, Dictionary<string, object> body_, string errorMessage_)
        {
            string bodyJson = GetSanitizedBodyJson(body_);
            AIProviderLogger.LogError($"[OpenRouter Image] Request failed - Model: {model_}, URL: {url_ ?? "(unknown)"}, Body: {bodyJson}, Error: {errorMessage_}");
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

        private Dictionary<string, string> GetHeaders(ImageRequestPayload requestPayload_)
        {
            string httpReferer = requestPayload_.OpenRouterRequestOptions?.HttpReferer;
            string appTitle = requestPayload_.OpenRouterRequestOptions?.AppTitle;

            return OpenRouterImageApiConfig.GetAuthHeaders(
                _settings.ApiKey, httpReferer, appTitle, _settings.CustomHeaders);
        }

        private Dictionary<string, object> BuildRequestBody(ImageRequestPayload requestPayload_, string model_)
        {
            string model = model_ ?? _settings.DefaultModel ?? OpenRouterImageApiConfig.DEFAULT_IMAGE_MODEL;

            string promptText = requestPayload_.Prompt;
            OpenRouterImageRequestOptions requestOptions = requestPayload_.OpenRouterRequestOptions;

            if (requestOptions != null)
            {
                if (!string.IsNullOrEmpty(requestOptions.Size))
                    promptText = $"{promptText} (size: {requestOptions.Size})";

                if (!string.IsNullOrEmpty(requestOptions.Quality))
                    promptText = $"{promptText} (quality: {requestOptions.Quality})";

                if (!string.IsNullOrEmpty(requestOptions.Style))
                    promptText = $"{promptText} (style: {requestOptions.Style})";
            }

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["role"] = "user",
                        ["content"] = promptText
                    }
                },
                ["modalities"] = new string[] { "image", "text" }
            };

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

            List<object> choices = JsonHelper.GetArray(json_, "choices");
            if (choices != null && choices.Count > 0)
            {
                Dictionary<string, object> firstChoice = choices[0] as Dictionary<string, object>;
                if (firstChoice != null)
                {
                    Dictionary<string, object> message = JsonHelper.GetObject(firstChoice, "message");
                    if (message != null)
                    {
                        string content = JsonHelper.GetValue<string>(message, "content");
                        if (!string.IsNullOrEmpty(content))
                        {
                            response.Content = content;
                            List<string> imageUrls = ExtractImageUrls(content);

                            for (int i = 0; i < imageUrls.Count; i++)
                            {
                                ImageResponseGeneratedImage image = new ImageResponseGeneratedImage
                                {
                                    Index = i,
                                    Url = imageUrls[i]
                                };
                                response.Images.Add(image);
                            }
                        }
                    }
                }
            }

            return response;
        }

        private List<string> ExtractImageUrls(string content_)
        {
            List<string> urls = new List<string>();
            if (string.IsNullOrEmpty(content_))
                return urls;

            string[] lines = content_.Split('\n');
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("![") && trimmedLine.Contains("](") && trimmedLine.EndsWith(")"))
                {
                    int startIndex = trimmedLine.IndexOf("](") + 2;
                    int endIndex = trimmedLine.LastIndexOf(")");
                    if (startIndex > 1 && endIndex > startIndex)
                    {
                        string url = trimmedLine.Substring(startIndex, endIndex - startIndex);
                        urls.Add(url);
                    }
                }
                else if (trimmedLine.StartsWith("http://") || trimmedLine.StartsWith("https://"))
                {
                    urls.Add(trimmedLine);
                }
            }

            return urls;
        }
    }
}
