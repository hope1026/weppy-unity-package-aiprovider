using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal class GoogleChatProvider : ChatProviderAbstract
    {
        internal override ChatProviderType ProviderType => ChatProviderType.GOOGLE;

        internal GoogleChatProvider(ChatProviderSettings settings_) : base(settings_, GoogleChatApiConfig.BASE_URL)
        {
        }

        internal override async Task<ChatResponse> SendMessageAsync(ChatRequestPayload requestPayload_, string model_, CancellationToken cancellationToken_ = default)
        {
            try
            {
                string model = model_ ?? _settings.DefaultModel ?? GoogleChatApiConfig.DEFAULT_CHAT_MODEL;
                Dictionary<string, object> body = BuildRequestBody(requestPayload_);
                string url = GoogleChatApiConfig.GetChatUrl(_settings.BaseUrl, model, _settings.ApiKey, false);
                Dictionary<string, string> headers = GoogleChatApiConfig.GetHeaders(_settings.CustomHeaders);

                HttpResponseResult response = await _httpClient.PostJsonAsync(url, body, headers, cancellationToken_);

                if (!response.IsSuccess)
                {
                    string errorMessage = response.ErrorMessage ?? $"HTTP {response.StatusCode}: {response.Content}";
                    LogRequestError(model, url, body, errorMessage);
                    return ChatResponse.FromError(errorMessage);
                }

                return ParseResponse(response.GetJsonContent(), model);
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel ?? GoogleChatApiConfig.DEFAULT_CHAT_MODEL, null, null, ex.Message);
                return ChatResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string model_, string url_, Dictionary<string, object> body_, string errorMessage_)
        {
            string bodyJson = GetSanitizedBodyJson(body_);
            AIProviderLogger.LogError($"[Google Chat] Request failed - Model: {model_}, URL: {url_ ?? "(unknown)"}, Body: {bodyJson}, Error: {errorMessage_}");
        }

        private string GetSanitizedBodyJson(Dictionary<string, object> body_)
        {
            if (body_ == null)
                return "(null)";

            try
            {
                Dictionary<string, object> sanitized = SanitizeBodyForLogging(body_);
                string json = JsonHelper.Serialize(sanitized);
                if (json != null && json.Length > 1000)
                    return json.Substring(0, 1000) + "...(truncated)";
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
                if (strValue.Length > 200)
                    return strValue.Substring(0, 200) + $"...(length:{strValue.Length})";
                return strValue;
            }

            if (value_ is Dictionary<string, object> dict)
            {
                if (dict.ContainsKey("data") || dict.ContainsKey("inlineData") || dict.ContainsKey("inline_data"))
                    return "(base64 data)";
                return SanitizeBodyForLogging(dict);
            }

            if (value_ is object[] arr)
            {
                if (arr.Length > 10)
                {
                    object[] truncatedArr = new object[10];
                    for (int i = 0; i < 10; i++)
                        truncatedArr[i] = SanitizeValue(arr[i]);
                    return new object[] { truncatedArr, $"...(total:{arr.Length})" };
                }
                object[] sanitizedArr = new object[arr.Length];
                for (int i = 0; i < arr.Length; i++)
                    sanitizedArr[i] = SanitizeValue(arr[i]);
                return sanitizedArr;
            }

            if (value_ is List<object> list)
            {
                if (list.Count > 10)
                {
                    List<object> truncatedList = new List<object>();
                    for (int i = 0; i < 10; i++)
                        truncatedList.Add(SanitizeValue(list[i]));
                    truncatedList.Add($"...(total:{list.Count})");
                    return truncatedList;
                }
                List<object> sanitizedList = new List<object>();
                foreach (object item in list)
                    sanitizedList.Add(SanitizeValue(item));
                return sanitizedList;
            }

            return value_;
        }

        internal override async Task StreamMessageAsync(
            ChatRequestPayload requestPayload_,
            string model_,
            Func<string, Task> onChunkReceived_,
            CancellationToken cancellationToken_ = default)
        {
            if (onChunkReceived_ == null)
            {
                Debug.LogError("GoogleChatProvider: onChunkReceived_ callback cannot be null for streaming.");
                return;
            }

            string model = model_ ?? _settings.DefaultModel ?? GoogleChatApiConfig.DEFAULT_CHAT_MODEL;
            Dictionary<string, object> body = BuildRequestBody(requestPayload_);
            string url = GoogleChatApiConfig.GetChatUrl(_settings.BaseUrl, model, _settings.ApiKey, true);
            Dictionary<string, string> headers = GoogleChatApiConfig.GetHeaders(_settings.CustomHeaders);

            await _httpClient.PostStreamWithCallbackAsync(url, body, async line =>
            {
                if (!TryExtractSseData(line, out string data))
                    return;

                Dictionary<string, object> json = JsonHelper.Deserialize(data);
                if (json == null)
                    return;

                List<object> candidates = JsonHelper.GetArray(json, "candidates");
                if (candidates == null || candidates.Count == 0)
                    return;

                Dictionary<string, object> candidate = candidates[0] as Dictionary<string, object>;
                if (candidate == null)
                    return;

                Dictionary<string, object> content = JsonHelper.GetObject(candidate, "content");
                if (content == null)
                    return;

                List<object> parts = JsonHelper.GetArray(content, "parts");
                if (parts == null || parts.Count == 0)
                    return;

                Dictionary<string, object> part = parts[0] as Dictionary<string, object>;
                if (part == null)
                    return;

                string text = JsonHelper.GetValue<string>(part, "text");
                if (!string.IsNullOrEmpty(text))
                    await onChunkReceived_(text);
            }, headers, cancellationToken_);
        }

        private Dictionary<string, object> BuildRequestBody(ChatRequestPayload requestPayload_)
        {
            List<object> contents = new List<object>();
            string systemInstruction = requestPayload_.SystemPrompt;

            foreach (ChatRequestMessage msg in requestPayload_.Messages)
            {
                if (msg == null)
                    continue;

                if (msg.RequestMessageRoleType == ChatRequestMessageRoleType.SYSTEM)
                {
                    systemInstruction = msg.Content;
                    continue;
                }

                string role = msg.RequestMessageRoleType == ChatRequestMessageRoleType.USER ? "user" : "model";
                List<object> parts = new List<object>();

                if (msg.MultiContent != null && msg.MultiContent.Count > 0)
                {
                    foreach (ChatRequestMessageContent content in msg.MultiContent)
                    {
                        if (content.Type == "text")
                        {
                            parts.Add(new Dictionary<string, object> { ["text"] = content.Text });
                        }
                        else if (IsImageContentPart(content.Type) && content.Image != null)
                        {
                            if (!string.IsNullOrEmpty(content.Image.Base64Data))
                            {
                                parts.Add(new Dictionary<string, object>
                                {
                                    ["inline_data"] = new Dictionary<string, object>
                                    {
                                        ["mime_type"] = content.Image.MediaType,
                                        ["data"] = content.Image.Base64Data
                                    }
                                });
                            }
                        }
                        else if (content.Type == "document" && content.Document != null)
                        {
                            if (!string.IsNullOrEmpty(content.Document.Base64Data))
                            {
                                parts.Add(new Dictionary<string, object>
                                {
                                    ["inline_data"] = new Dictionary<string, object>
                                    {
                                        ["mime_type"] = content.Document.MediaType,
                                        ["data"] = content.Document.Base64Data
                                    }
                                });
                            }
                        }
                        else if (content.Type == "text_file" && content.Document != null)
                        {
                            if (!string.IsNullOrEmpty(content.Document.TextContent))
                            {
                                string fileLabel = !string.IsNullOrEmpty(content.Document.FileName)
                                    ? $"[File: {content.Document.FileName}]\n"
                                    : "";
                                parts.Add(new Dictionary<string, object> { ["text"] = fileLabel + content.Document.TextContent });
                            }
                        }
                    }
                }
                else
                {
                    parts.Add(new Dictionary<string, object> { ["text"] = msg.Content });
                }

                contents.Add(new Dictionary<string, object>
                {
                    ["role"] = role,
                    ["parts"] = parts.ToArray()
                });
            }

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["contents"] = contents.ToArray()
            };

            if (!string.IsNullOrEmpty(systemInstruction))
            {
                body["systemInstruction"] = new Dictionary<string, object>
                {
                    ["parts"] = new object[]
                    {
                        new Dictionary<string, object> { ["text"] = systemInstruction }
                    }
                };
            }

            Dictionary<string, object> generationConfig = new Dictionary<string, object>();

            if (requestPayload_.Temperature.HasValue)
                generationConfig["temperature"] = requestPayload_.Temperature.Value;

            if (requestPayload_.MaxTokens.HasValue)
                generationConfig["maxOutputTokens"] = requestPayload_.MaxTokens.Value;

            if (requestPayload_.TopP.HasValue)
                generationConfig["topP"] = requestPayload_.TopP.Value;

            if (requestPayload_.Stop != null && requestPayload_.Stop.Count > 0)
                generationConfig["stopSequences"] = requestPayload_.Stop.ToArray();

            GoogleChatRequestOptions options = requestPayload_.GoogleOptions;
            if (options != null)
            {
                if (options.TopK.HasValue)
                    generationConfig["topK"] = options.TopK.Value;

                if (options.CandidateCount.HasValue)
                    generationConfig["candidateCount"] = options.CandidateCount.Value;

                if (options.SafetySettings != null && options.SafetySettings.Count > 0)
                {
                    List<object> safetySettingsList = new List<object>();
                    foreach (GoogleChatRequestOptions.SafetySetting setting in options.SafetySettings)
                    {
                        safetySettingsList.Add(new Dictionary<string, object>
                        {
                            ["category"] = setting.Category,
                            ["threshold"] = setting.Threshold
                        });
                    }

                    body["safetySettings"] = safetySettingsList.ToArray();
                }
            }

            if (generationConfig.Count > 0)
                body["generationConfig"] = generationConfig;

            if (requestPayload_.AdditionalBodyParameters != null)
            {
                foreach (KeyValuePair<string, object> kvp in requestPayload_.AdditionalBodyParameters)
                {
                    body[kvp.Key] = kvp.Value;
                }
            }

            return body;
        }

        private ChatResponse ParseResponse(Dictionary<string, object> json_, string model_)
        {
            if (json_ == null)
                return ChatResponse.FromError("Invalid response format");

            ChatResponse response = new ChatResponse
            {
                Model = model_,
                RawResponse = json_,
                GoogleData = new GoogleChatResponseData()
            };

            List<object> candidates = JsonHelper.GetArray(json_, "candidates");
            if (candidates != null && candidates.Count > 0)
            {
                Dictionary<string, object> candidate = candidates[0] as Dictionary<string, object>;
                if (candidate != null)
                {
                    Dictionary<string, object> content = JsonHelper.GetObject(candidate, "content");
                    if (content != null)
                    {
                        List<object> parts = JsonHelper.GetArray(content, "parts");
                        if (parts != null && parts.Count > 0)
                        {
                            Dictionary<string, object> part = parts[0] as Dictionary<string, object>;
                            if (part != null)
                                response.Content = JsonHelper.GetValue<string>(part, "text");
                        }
                    }

                    response.FinishReason = JsonHelper.GetValue<string>(candidate, "finishReason");

                    List<object> safetyRatings = JsonHelper.GetArray(candidate, "safetyRatings");
                    if (safetyRatings != null && safetyRatings.Count > 0)
                    {
                        response.GoogleData.SafetyRatings = new List<GoogleChatResponseData.SafetyRating>();
                        foreach (object item in safetyRatings)
                        {
                            Dictionary<string, object> rating = item as Dictionary<string, object>;
                            if (rating != null)
                            {
                                response.GoogleData.SafetyRatings.Add(new GoogleChatResponseData.SafetyRating(
                                    JsonHelper.GetValue<string>(rating, "category"),
                                    JsonHelper.GetValue<string>(rating, "probability"),
                                    JsonHelper.GetValue<bool>(rating, "blocked")
                                ));
                            }
                        }
                    }
                }
            }

            Dictionary<string, object> promptFeedback = JsonHelper.GetObject(json_, "promptFeedback");
            if (promptFeedback != null)
            {
                response.GoogleData.PromptFeedback = new GoogleChatResponseData.PromptFeedbackData
                {
                    BlockReason = JsonHelper.GetValue<string>(promptFeedback, "blockReason")
                };

                List<object> feedbackRatings = JsonHelper.GetArray(promptFeedback, "safetyRatings");
                if (feedbackRatings != null && feedbackRatings.Count > 0)
                {
                    response.GoogleData.PromptFeedback.SafetyRatings = new List<GoogleChatResponseData.SafetyRating>();
                    foreach (object item in feedbackRatings)
                    {
                        Dictionary<string, object> rating = item as Dictionary<string, object>;
                        if (rating != null)
                        {
                            response.GoogleData.PromptFeedback.SafetyRatings.Add(new GoogleChatResponseData.SafetyRating(
                                JsonHelper.GetValue<string>(rating, "category"),
                                JsonHelper.GetValue<string>(rating, "probability"),
                                JsonHelper.GetValue<bool>(rating, "blocked")
                            ));
                        }
                    }
                }
            }

            Dictionary<string, object> usageMetadata = JsonHelper.GetObject(json_, "usageMetadata");
            if (usageMetadata != null)
            {
                response.Usage = new ChatResponseUsageInfo
                {
                    PromptTokens = (int)JsonHelper.GetValue<long>(usageMetadata, "promptTokenCount"),
                    CompletionTokens = (int)JsonHelper.GetValue<long>(usageMetadata, "candidatesTokenCount"),
                    TotalTokens = (int)JsonHelper.GetValue<long>(usageMetadata, "totalTokenCount")
                };
            }

            return response;
        }
    }
}
