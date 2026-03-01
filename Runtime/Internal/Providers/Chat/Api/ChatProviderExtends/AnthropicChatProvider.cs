using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal class AnthropicChatProvider : ChatProviderAbstract
    {
        internal override ChatProviderType ProviderType => ChatProviderType.ANTHROPIC;

        internal AnthropicChatProvider(ChatProviderSettings settings_) : base(settings_, AnthropicChatApiConfig.BASE_URL)
        {
        }

        internal override async Task<ChatResponse> SendMessageAsync(ChatRequestPayload requestPayload_, string model_, CancellationToken cancellationToken_ = default)
        {
            try
            {
                Dictionary<string, object> body = BuildRequestBody(requestPayload_, model_, isStream_:false);
                string url = $"{_settings.BaseUrl}{AnthropicChatApiConfig.CHAT_ENDPOINT}";
                Dictionary<string, string> headers = AnthropicChatApiConfig.GetAuthHeaders(_settings.ApiKey, _settings.CustomHeaders);

                HttpResponseResult response = await _httpClient.PostJsonAsync(url, body, headers, cancellationToken_);

                if (!response.IsSuccess)
                {
                    string errorMessage = response.ErrorMessage ?? $"HTTP {response.StatusCode}: {response.Content}";
                    LogRequestError(model_ ?? _settings.DefaultModel ?? AnthropicChatApiConfig.DEFAULT_CHAT_MODEL, url, body, errorMessage);
                    return ChatResponse.FromError(errorMessage);
                }

                return ParseResponse(response.GetJsonContent());
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel ?? AnthropicChatApiConfig.DEFAULT_CHAT_MODEL, null, null, ex.Message);
                return ChatResponse.FromError(ex.Message);
            }
        }

        private void LogRequestError(string model_, string url_, Dictionary<string, object> body_, string errorMessage_)
        {
            string bodyJson = GetSanitizedBodyJson(body_);
            AIProviderLogger.LogError($"[Anthropic Chat] Request failed - Model: {model_}, URL: {url_ ?? "(unknown)"}, Body: {bodyJson}, Error: {errorMessage_}");
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
                if (dict.ContainsKey("data") || dict.ContainsKey("source"))
                {
                    if (dict.ContainsKey("source") && dict["source"] is Dictionary<string, object> source && source.ContainsKey("data"))
                        return "(base64 data)";
                }
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
                Debug.LogError("AnthropicChatProvider: onChunkReceived_ callback cannot be null for streaming.");
                return;
            }

            Dictionary<string, object> body = BuildRequestBody(requestPayload_, model_, isStream_:true);
            string url = $"{_settings.BaseUrl}{AnthropicChatApiConfig.CHAT_ENDPOINT}";
            Dictionary<string, string> headers = AnthropicChatApiConfig.GetAuthHeaders(_settings.ApiKey, _settings.CustomHeaders);

            await _httpClient.PostStreamWithCallbackAsync(url, body, async line =>
            {
                if (!TryExtractSseData(line, out string data))
                    return;

                Dictionary<string, object> json = JsonHelper.Deserialize(data);
                if (json == null)
                    return;

                string eventType = JsonHelper.GetValue<string>(json, "type");

                if (eventType == "content_block_delta")
                {
                    Dictionary<string, object> delta = JsonHelper.GetObject(json, "delta");
                    if (delta != null)
                    {
                        string text = JsonHelper.GetValue<string>(delta, "text");
                        if (!string.IsNullOrEmpty(text))
                            await onChunkReceived_(text);
                    }
                }
            }, headers, cancellationToken_);
        }

        private Dictionary<string, object> BuildRequestBody(ChatRequestPayload requestPayload_, string model_, bool isStream_)
        {
            List<object> messages = new List<object>();
            string systemPrompt = requestPayload_.SystemPrompt;

            foreach (ChatRequestMessage msg in requestPayload_.Messages)
            {
                if (msg == null)
                    continue;

                if (msg.RequestMessageRoleType == ChatRequestMessageRoleType.SYSTEM)
                {
                    systemPrompt = msg.Content;
                    continue;
                }

                string role = msg.RequestMessageRoleType == ChatRequestMessageRoleType.USER ? "user" : "assistant";

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
                        else if (IsImageContentPart(part.Type) && part.Image != null)
                        {
                            if (!string.IsNullOrEmpty(part.Image.Base64Data))
                            {
                                contentParts.Add(new Dictionary<string, object>
                                {
                                    ["type"] = "image",
                                    ["source"] = new Dictionary<string, object>
                                    {
                                        ["type"] = "base64",
                                        ["media_type"] = part.Image.MediaType,
                                        ["data"] = part.Image.Base64Data
                                    }
                                });
                            }
                            else if (!string.IsNullOrEmpty(part.Image.Url))
                            {
                                contentParts.Add(new Dictionary<string, object>
                                {
                                    ["type"] = "image",
                                    ["source"] = new Dictionary<string, object>
                                    {
                                        ["type"] = "url",
                                        ["url"] = part.Image.Url
                                    }
                                });
                            }
                        }
                        else if (part.Type == "document" && part.Document != null)
                        {
                            if (!string.IsNullOrEmpty(part.Document.Base64Data))
                            {
                                contentParts.Add(new Dictionary<string, object>
                                {
                                    ["type"] = "document",
                                    ["source"] = new Dictionary<string, object>
                                    {
                                        ["type"] = "base64",
                                        ["media_type"] = part.Document.MediaType,
                                        ["data"] = part.Document.Base64Data
                                    }
                                });
                            }
                        }
                        else if (part.Type == "text_file" && part.Document != null)
                        {
                            if (!string.IsNullOrEmpty(part.Document.TextContent))
                            {
                                string fileLabel = !string.IsNullOrEmpty(part.Document.FileName)
                                    ? $"[File: {part.Document.FileName}]\n"
                                    : "";
                                contentParts.Add(new Dictionary<string, object>
                                {
                                    ["type"] = "text",
                                    ["text"] = fileLabel + part.Document.TextContent
                                });
                            }
                        }
                    }

                    messages.Add(new Dictionary<string, object>
                    {
                        ["role"] = role,
                        ["content"] = contentParts.ToArray()
                    });
                }
                else
                {
                    messages.Add(new Dictionary<string, object>
                    {
                        ["role"] = role,
                        ["content"] = msg.Content
                    });
                }
            }

            Dictionary<string, object> body = new Dictionary<string, object>
            {
                ["model"] = model_ ?? _settings.DefaultModel ?? AnthropicChatApiConfig.DEFAULT_CHAT_MODEL,
                ["max_tokens"] = requestPayload_.MaxTokens ?? AnthropicChatApiConfig.DEFAULT_MAX_TOKENS,
                ["messages"] = messages.ToArray()
            };

            if (!string.IsNullOrEmpty(systemPrompt))
                body["system"] = systemPrompt;

            if (requestPayload_.Temperature.HasValue)
                body["temperature"] = requestPayload_.Temperature.Value;

            if (requestPayload_.TopP.HasValue)
                body["top_p"] = requestPayload_.TopP.Value;

            if (requestPayload_.Stop != null && requestPayload_.Stop.Count > 0)
                body["stop_sequences"] = requestPayload_.Stop.ToArray();

            if (isStream_)
                body["stream"] = true;

            AnthropicChatRequestOptions options = requestPayload_.AnthropicOptions;
            if (options != null)
            {
                if (options.TopK.HasValue)
                    body["top_k"] = options.TopK.Value;

                if (options.Metadata != null && options.Metadata.Count > 0)
                    body["metadata"] = options.Metadata;
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
                FinishReason = JsonHelper.GetValue<string>(json_, "stop_reason"),
                RawResponse = json_,
                AnthropicData = new AnthropicChatResponseData
                {
                    StopSequence = JsonHelper.GetValue<string>(json_, "stop_sequence")
                }
            };

            List<object> content = JsonHelper.GetArray(json_, "content");
            if (content != null && content.Count > 0)
            {
                List<string> textParts = new List<string>();
                foreach (object item in content)
                {
                    Dictionary<string, object> block = item as Dictionary<string, object>;
                    if (block == null)
                        continue;

                    string type = JsonHelper.GetValue<string>(block, "type");
                    if (type == "text")
                    {
                        string text = JsonHelper.GetValue<string>(block, "text");
                        if (!string.IsNullOrEmpty(text))
                            textParts.Add(text);
                    }
                }

                response.Content = string.Join("", textParts);
            }

            Dictionary<string, object> usage = JsonHelper.GetObject(json_, "usage");
            if (usage != null)
            {
                int inputTokens = (int)JsonHelper.GetValue<long>(usage, "input_tokens");
                int outputTokens = (int)JsonHelper.GetValue<long>(usage, "output_tokens");
                response.Usage = new ChatResponseUsageInfo(inputTokens, outputTokens);

                long cacheCreation = JsonHelper.GetValue<long>(usage, "cache_creation_input_tokens");
                if (cacheCreation > 0)
                    response.AnthropicData.CacheCreationInputTokens = (int)cacheCreation;

                long cacheRead = JsonHelper.GetValue<long>(usage, "cache_read_input_tokens");
                if (cacheRead > 0)
                    response.AnthropicData.CacheReadInputTokens = (int)cacheRead;
            }

            return response;
        }
    }
}
