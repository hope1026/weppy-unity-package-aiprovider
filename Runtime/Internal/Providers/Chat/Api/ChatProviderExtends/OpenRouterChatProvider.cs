using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider.Chat
{
    internal class OpenRouterChatProvider : ChatProviderAbstract
    {
        internal override ChatProviderType ProviderType => ChatProviderType.OPEN_ROUTER;

        internal OpenRouterChatProvider(ChatProviderSettings settings_) : base(settings_, OpenRouterChatApiConfig.BASE_URL)
        {
        }

        internal override async Task<ChatResponse> SendMessageAsync(ChatRequestPayload requestPayload_, string model_, CancellationToken cancellationToken_ = default)
        {
            try
            {
                Dictionary<string, object> body = BuildRequestBody(requestPayload_, model_, isStream_: false);
                string url = $"{_settings.BaseUrl}{OpenRouterChatApiConfig.CHAT_ENDPOINT}";
                Dictionary<string, string> headers = GetHeaders(requestPayload_);

                HttpResponseResult response = await _httpClient.PostJsonAsync(url, body, headers, cancellationToken_);

                if (!response.IsSuccess)
                {
                    string errorMessage = response.ErrorMessage ?? $"HTTP {response.StatusCode}: {response.Content}";
                    LogRequestError(model_ ?? _settings.DefaultModel ?? OpenRouterChatApiConfig.DEFAULT_CHAT_MODEL, url, body, errorMessage);
                    return ChatResponse.FromError(errorMessage);
                }

                return ParseResponse(response.GetJsonContent());
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel ?? OpenRouterChatApiConfig.DEFAULT_CHAT_MODEL, null, null, ex.Message);
                return ChatResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string model_, string url_, Dictionary<string, object> body_, string errorMessage_)
        {
            string bodyJson = GetSanitizedBodyJson(body_);
            AIProviderLogger.LogError($"[OpenRouter Chat] Request failed - Model: {model_}, URL: {url_ ?? "(unknown)"}, Body: {bodyJson}, Error: {errorMessage_}");
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
                if (dict.ContainsKey("data") || dict.ContainsKey("image_url"))
                    return "(base64/image data)";
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
                Debug.LogError("OpenRouterChatProvider: onChunkReceived_ callback cannot be null for streaming.");
                return;
            }

            Dictionary<string, object> body = BuildRequestBody(requestPayload_, model_, isStream_: true);
            string url = $"{_settings.BaseUrl}{OpenRouterChatApiConfig.CHAT_ENDPOINT}";
            Dictionary<string, string> headers = GetHeaders(requestPayload_);

            await _httpClient.PostStreamWithCallbackAsync(url, body, async line =>
            {
                if (string.IsNullOrEmpty(line) || !line.StartsWith("options: "))
                    return;

                string data = line.Substring(6);
                if (data == "[DONE]")
                    return;

                Dictionary<string, object> json = JsonHelper.Deserialize(data);
                if (json == null)
                    return;

                List<object> choices = JsonHelper.GetArray(json, "choices");
                if (choices == null || choices.Count == 0)
                    return;

                Dictionary<string, object> choice = choices[0] as Dictionary<string, object>;
                if (choice == null)
                    return;

                Dictionary<string, object> delta = JsonHelper.GetObject(choice, "delta");
                if (delta == null)
                    return;

                string content = JsonHelper.GetValue<string>(delta, "content");
                if (!string.IsNullOrEmpty(content))
                    await onChunkReceived_(content);
            }, headers, cancellationToken_);
        }

        private Dictionary<string, string> GetHeaders(ChatRequestPayload requestPayload_)
        {
            string httpReferer = requestPayload_.OpenRouterOptions?.HttpReferer;
            string appTitle = requestPayload_.OpenRouterOptions?.AppTitle;

            return OpenRouterChatApiConfig.GetAuthHeaders(
                _settings.ApiKey, httpReferer, appTitle, _settings.CustomHeaders);
        }

        private Dictionary<string, object> BuildRequestBody(ChatRequestPayload requestPayload_, string model_, bool isStream_)
        {
            List<object> messages = new List<object>();

            if (!string.IsNullOrEmpty(requestPayload_.SystemPrompt))
            {
                messages.Add(new Dictionary<string, object>
                {
                    ["role"] = "system",
                    ["content"] = requestPayload_.SystemPrompt
                });
            }

            foreach (ChatRequestMessage msg in requestPayload_.Messages)
            {
                Dictionary<string, object> message = new Dictionary<string, object>
                {
                    ["role"] = msg.GetRoleString()
                };

                if (msg.MultiContent != null && msg.MultiContent.Count > 0)
                {
                    List<object> contentParts = new List<object>();
                    foreach (ChatRequestMessageContent part in msg.MultiContent)
                    {
                        if (part.Type == "text")
                        {
                            contentParts.Add(new Dictionary<string, object>
                            {
                                ["type"] = "text",
                                ["text"] = part.Text
                            });
                        }
                        else if (part.Type == "image" && part.Image != null)
                        {
                            string imageUrl = !string.IsNullOrEmpty(part.Image.Url)
                                ? part.Image.Url
                                : $"options:{part.Image.MediaType};base64,{part.Image.Base64Data}";

                            contentParts.Add(new Dictionary<string, object>
                            {
                                ["type"] = "image_url",
                                ["image_url"] = new Dictionary<string, object>
                                {
                                    ["url"] = imageUrl
                                }
                            });
                        }
                    }

                    message["content"] = contentParts.ToArray();
                }
                else
                {
                    message["content"] = msg.Content;
                }

                messages.Add(message);
            }

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["model"] = model_ ?? _settings.DefaultModel ?? OpenRouterChatApiConfig.DEFAULT_CHAT_MODEL,
                ["messages"] = messages.ToArray()
            };

            if (requestPayload_.Temperature.HasValue)
                body["temperature"] = requestPayload_.Temperature.Value;

            if (requestPayload_.MaxTokens.HasValue)
                body["max_tokens"] = requestPayload_.MaxTokens.Value;

            if (requestPayload_.TopP.HasValue)
                body["top_p"] = requestPayload_.TopP.Value;

            if (requestPayload_.Stop != null && requestPayload_.Stop.Count > 0)
                body["stop"] = requestPayload_.Stop.ToArray();

            if (isStream_)
                body["stream"] = true;

            OpenRouterChatRequestOptions options = requestPayload_.OpenRouterOptions;
            if (options != null)
            {
                if (options.FrequencyPenalty.HasValue)
                    body["frequency_penalty"] = options.FrequencyPenalty.Value;

                if (options.PresencePenalty.HasValue)
                    body["presence_penalty"] = options.PresencePenalty.Value;

                if (options.TopK.HasValue)
                    body["top_k"] = options.TopK.Value;

                if (options.Seed.HasValue)
                    body["seed"] = options.Seed.Value;

                if (!string.IsNullOrEmpty(options.ResponseFormat))
                    body["response_format"] = new Dictionary<string, string> { ["type"] = options.ResponseFormat };

                if (options.LogitBias != null && options.LogitBias.Count > 0)
                    body["logit_bias"] = options.LogitBias;

                if (!string.IsNullOrEmpty(options.User))
                    body["user"] = options.User;

                if (options.N.HasValue)
                    body["n"] = options.N.Value;

                if (options.Transforms != null && options.Transforms.Count > 0)
                    body["transforms"] = options.Transforms.ToArray();

                if (options.Models != null && options.Models.Count > 0)
                    body["models"] = options.Models.ToArray();

                if (!string.IsNullOrEmpty(options.Route))
                    body["route"] = options.Route;

                if (options.Provider != null)
                    body["provider"] = options.Provider;
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

        private ChatResponse ParseResponse(Dictionary<string, object> json_)
        {
            if (json_ == null)
                return ChatResponse.FromError("Invalid response format");

            ChatResponse response = new ChatResponse
            {
                Id = JsonHelper.GetValue<string>(json_, "id"),
                Model = JsonHelper.GetValue<string>(json_, "model"),
                RawResponse = json_
            };

            List<object> choices = JsonHelper.GetArray(json_, "choices");
            if (choices != null && choices.Count > 0)
            {
                Dictionary<string, object> choice = choices[0] as Dictionary<string, object>;
                if (choice != null)
                {
                    Dictionary<string, object> message = JsonHelper.GetObject(choice, "message");
                    if (message != null)
                    {
                        response.Content = JsonHelper.GetValue<string>(message, "content");
                    }

                    response.FinishReason = JsonHelper.GetValue<string>(choice, "finish_reason");
                }
            }

            Dictionary<string, object> usage = JsonHelper.GetObject(json_, "usage");
            if (usage != null)
            {
                response.Usage = new ChatResponseUsageInfo
                {
                    PromptTokens = (int)JsonHelper.GetValue<long>(usage, "prompt_tokens"),
                    CompletionTokens = (int)JsonHelper.GetValue<long>(usage, "completion_tokens"),
                    TotalTokens = (int)JsonHelper.GetValue<long>(usage, "total_tokens")
                };
            }

            return response;
        }
    }
}
