using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal class ImageProviderGoogleGemini : ImageProviderAbstract
    {
        internal override ImageProviderType ProviderType => ImageProviderType.GOOGLE_GEMINI;

        internal ImageProviderGoogleGemini(ImageProviderSettings settings_) : base(settings_, GoogleImageApiConfig.BASE_URL)
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
                string model = model_ ?? _settings.DefaultModel ?? GoogleImageApiConfig.DEFAULT_GEMINI_IMAGE_MODEL;
                Dictionary<string, object> body = BuildRequestBody(requestPayload_);
                string url = GoogleImageApiConfig.GetGeminiImageUrl(_settings.BaseUrl, model, _settings.ApiKey);
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
                LogRequestError(model_ ?? _settings.DefaultModel ?? GoogleImageApiConfig.DEFAULT_GEMINI_IMAGE_MODEL, null, null, ex.Message);
                return ImageResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string model_, string url_, Dictionary<string, object> body_, string errorMessage_)
        {
            string bodyJson = GetSanitizedBodyJson(body_);
            AIProviderLogger.LogError($"[GoogleGemini Image] Request failed - Model: {model_}, URL: {url_ ?? "(unknown)"}, Body: {bodyJson}, Error: {errorMessage_}");
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
            List<object> parts = new List<object>();

            if (requestPayload_.InputImages != null && requestPayload_.InputImages.Count > 0)
            {
                foreach (ImageRequestInputData inputImage in requestPayload_.InputImages)
                {
                    parts.Add(new Dictionary<string, object>
                    {
                        ["inlineData"] = new Dictionary<string, object>
                        {
                            ["mimeType"] = inputImage.MediaType ?? "image/png",
                            ["data"] = inputImage.Base64Data
                        }
                    });
                }
            }

            parts.Add(new Dictionary<string, object>
            {
                ["text"] = requestPayload_.Prompt
            });

            Dictionary<string, object> generationConfig = new Dictionary<string, object>
            {
                ["responseModalities"] = new string[] { "TEXT", "IMAGE" }
            };

            GoogleGeminiImageRequestOptions options = requestPayload_.GoogleGeminiOptions;
            if (options != null)
            {
                if (!string.IsNullOrEmpty(options.AspectRatio))
                    generationConfig["aspectRatio"] = options.AspectRatio;

                if (!string.IsNullOrEmpty(options.Resolution))
                {
                    generationConfig["outputImageSettings"] = new Dictionary<string, object>
                    {
                        ["outputResolution"] = options.Resolution
                    };
                }
            }

            // Handle AdditionalBodyParameters (from dynamic options in Editor)
            if (requestPayload_.AdditionalBodyParameters != null)
            {
                foreach (KeyValuePair<string, object> kvp in requestPayload_.AdditionalBodyParameters)
                {
                    if (kvp.Key == "aspectRatio" && !string.IsNullOrEmpty(kvp.Value?.ToString()))
                    {
                        generationConfig["aspectRatio"] = kvp.Value.ToString();
                    }
                    else if (kvp.Key == "outputResolution" && !string.IsNullOrEmpty(kvp.Value?.ToString()))
                    {
                        generationConfig["outputImageSettings"] = new Dictionary<string, object>
                        {
                            ["outputResolution"] = kvp.Value.ToString()
                        };
                    }
                    // Ignore other parameters (e.g., size, quality, style from OpenAI)
                }
            }

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["contents"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["parts"] = parts.ToArray()
                    }
                },
                ["generationConfig"] = generationConfig
            };

            return body;
        }

        private ImageResponse ParseResponse(Dictionary<string, object> json_, string model_)
        {
            if (json_ == null)
            {
                Debug.LogError("[AIProvider] ParseResponse: json_ is null");
                return ImageResponse.FromError("Invalid response format");
            }

            Debug.Log($"[AIProvider] ParseResponse: Raw JSON keys: {string.Join(", ", json_.Keys)}");

            ImageResponse response = new ImageResponse
            {
                Model = model_,
                RawResponse = json_
            };

            List<object> candidates = JsonHelper.GetArray(json_, "candidates");
            Debug.Log($"[AIProvider] ParseResponse: candidates count = {candidates?.Count ?? 0}");

            if (candidates != null)
            {
                int imageIndex = 0;
                foreach (object candidateObj in candidates)
                {
                    Dictionary<string, object> candidate = candidateObj as Dictionary<string, object>;
                    if (candidate == null)
                    {
                        Debug.LogWarning("[AIProvider] ParseResponse: candidate is null");
                        continue;
                    }

                    Debug.Log($"[AIProvider] ParseResponse: candidate keys: {string.Join(", ", candidate.Keys)}");

                    Dictionary<string, object> content = JsonHelper.GetObject(candidate, "content");
                    if (content == null)
                    {
                        Debug.LogWarning("[AIProvider] ParseResponse: content is null");
                        continue;
                    }

                    List<object> parts = JsonHelper.GetArray(content, "parts");
                    Debug.Log($"[AIProvider] ParseResponse: parts count = {parts?.Count ?? 0}");

                    if (parts == null)
                        continue;

                    foreach (object partObj in parts)
                    {
                        Dictionary<string, object> part = partObj as Dictionary<string, object>;
                        if (part == null)
                            continue;

                        Debug.Log($"[AIProvider] ParseResponse: part keys: {string.Join(", ", part.Keys)}");

                        string text = JsonHelper.GetValue<string>(part, "text");
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (string.IsNullOrEmpty(response.Content))
                            {
                                response.Content = text;
                            }
                            else
                            {
                                response.Content += "\n" + text;
                            }
                            continue;
                        }

                        Dictionary<string, object> inlineData = JsonHelper.GetObject(part, "inlineData");
                        if (inlineData == null)
                        {
                            inlineData = JsonHelper.GetObject(part, "inline_data");
                        }

                        if (inlineData == null)
                        {
                            Debug.LogWarning("[AIProvider] ParseResponse: inlineData is null, checking for other keys");
                            continue;
                        }

                        Debug.Log($"[AIProvider] ParseResponse: inlineData keys: {string.Join(", ", inlineData.Keys)}");

                        string base64Data = JsonHelper.GetValue<string>(inlineData, "data");
                        if (string.IsNullOrEmpty(base64Data))
                        {
                            Debug.LogWarning("[AIProvider] ParseResponse: base64Data is empty");
                            continue;
                        }

                        Debug.Log($"[AIProvider] ParseResponse: Found image options, length = {base64Data.Length}");

                        ImageResponseGeneratedImage image = new ImageResponseGeneratedImage
                        {
                            Index = imageIndex++,
                            Base64Data = base64Data
                        };

                        response.Images.Add(image);
                    }
                }
            }

            Debug.Log($"[AIProvider] ParseResponse: Total images found = {response.Images.Count}");
            return response;
        }
    }
}
