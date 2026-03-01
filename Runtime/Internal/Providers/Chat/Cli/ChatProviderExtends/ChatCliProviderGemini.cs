using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal class ChatCliProviderGemini : ChatCliProviderAbstract
    {
        private readonly GeminiCliWrapper _cliWrapper;

        internal override ChatCliProviderType ProviderType => ChatCliProviderType.GEMINI_CLI;
        internal override bool SupportsPersistentSession => true;

        internal ChatCliProviderGemini(ChatCliProviderSettings settings_) : base(settings_)
        {
            _cliWrapper = new GeminiCliWrapper(_settings.TimeoutSeconds);
        }

        internal override async Task<ChatCliResponse> SendMessageAsync(
            ChatCliRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return ChatCliResponse.FromError("Request cannot be null.");

            if (!IsPlatformSupported())
                return ChatCliResponse.FromError("Gemini CLI is supported on Windows/macOS/Linux editors and desktop standalone platforms.");

            if (requestPayload_.Messages == null || requestPayload_.Messages.Count == 0)
                return ChatCliResponse.FromError("Messages cannot be empty.");

            try
            {
                string model = model_ ?? _settings.DefaultModel;
                if (model == GeminiCliWrapper.AUTO_MODEL_ID)
                    model = string.Empty;

                string promptText = BuildPromptText(requestPayload_);

                string cliExecutable = string.IsNullOrEmpty(_settings.CliExecutablePath)
                    ? GeminiChatCliConfig.DEFAULT_EXECUTABLE_NAME
                    : _settings.CliExecutablePath;

                string cliArgumentsBase = string.IsNullOrEmpty(_settings.CliArguments)
                    ? GeminiChatCliConfig.DEFAULT_CHAT_ARGUMENTS
                    : _settings.CliArguments;

                string cliArguments = BuildCliArguments(cliArgumentsBase, model, promptText);

                Dictionary<string, string> environment = BuildEnvironmentVariables();

                GeminiCliResponseResult result = await _cliWrapper.ExecuteAsync(
                    cliExecutable,
                    cliArguments,
                    null,
                    environment,
                    _settings.CliWorkingDirectory,
                    cancellationToken_);

                if (!result.IsSuccess)
                {
                    string error = result.ErrorMessage ?? result.StandardError ?? "Gemini CLI request failed.";
                    LogRequestError(model, cliExecutable, cliArguments, promptText, error);
                    return ChatCliResponse.FromError(error);
                }

                ChatCliResponse response = ParseGeminiResponse(result.StandardOutput, model);
                response.ProviderType = ProviderType;
                return response;
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel, null, null, null, ex.Message);
                return ChatCliResponse.FromError(ex.Message);
            }
        }

        internal override async Task<ChatCliResponse> SendPersistentMessageAsync(
            ChatCliRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return ChatCliResponse.FromError("Request cannot be null.");

            if (!IsPlatformSupported())
                return ChatCliResponse.FromError("Gemini CLI is supported on Windows/macOS/Linux editors and desktop standalone platforms.");

            if (requestPayload_.Messages == null || requestPayload_.Messages.Count == 0)
                return ChatCliResponse.FromError("Messages cannot be empty.");

            try
            {
                string model = model_ ?? _settings.DefaultModel;
                if (model == GeminiCliWrapper.AUTO_MODEL_ID)
                    model = string.Empty;

                string lastUserMessage = ExtractLastUserMessage(requestPayload_);
                if (string.IsNullOrEmpty(lastUserMessage))
                    return ChatCliResponse.FromError("No user message found in request.");

                string cliExecutable = string.IsNullOrEmpty(_settings.CliExecutablePath)
                    ? GeminiChatCliConfig.DEFAULT_EXECUTABLE_NAME
                    : _settings.CliExecutablePath;

                string cliArguments = BuildPersistentCliArguments(model);

                Dictionary<string, string> environment = BuildEnvironmentVariables();

                GeminiCliResponseResult result = await _cliWrapper.ExecutePersistentAsync(
                    cliExecutable,
                    cliArguments,
                    lastUserMessage,
                    environment,
                    _settings.CliWorkingDirectory,
                    cancellationToken_);

                if (!result.IsSuccess)
                {
                    string error = result.ErrorMessage ?? result.StandardError ?? "Gemini CLI persistent request failed.";
                    LogRequestError(model, cliExecutable, cliArguments, lastUserMessage, error);
                    return ChatCliResponse.FromError(error);
                }

                ChatCliResponse response = ParseStreamJsonResponse(result.StandardOutput, model);
                response.ProviderType = ProviderType;
                return response;
            }
            catch (Exception ex)
            {
                LogRequestError(model_ ?? _settings.DefaultModel, null, null, null, ex.Message);
                return ChatCliResponse.FromError(ex.Message);
            }
        }

        internal override void ResetSession()
        {
            _cliWrapper.ResetPersistentProcessPublic();
        }

        private string ExtractLastUserMessage(ChatCliRequestPayload requestPayload_)
        {
            if (requestPayload_.Messages == null || requestPayload_.Messages.Count == 0)
                return null;

            for (int i = requestPayload_.Messages.Count - 1; i >= 0; i--)
            {
                ChatCliRequestMessage message = requestPayload_.Messages[i];
                if (message != null && message.RequestMessageRoleType == ChatCliRequestMessageRoleType.USER)
                    return message.Content;
            }

            return null;
        }

        private string BuildPersistentCliArguments(string model_)
        {
            string arguments = GeminiChatCliConfig.DEFAULT_PERSISTENT_ARGUMENTS;
            arguments = EnsureModelArgument(arguments, model_);
            return arguments.Trim();
        }

        private ChatCliResponse ParseStreamJsonResponse(string jsonlOutput_, string model_)
        {
            if (string.IsNullOrEmpty(jsonlOutput_))
                return ChatCliResponse.FromError("Gemini CLI persistent response was empty.");

            ChatCliResponse response = new ChatCliResponse
            {
                Model = model_
            };

            System.Text.StringBuilder contentBuilder = new System.Text.StringBuilder();
            string[] lines = jsonlOutput_.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    Dictionary<string, object> eventData = JsonHelper.Deserialize(line);
                    if (eventData == null)
                        continue;

                    string eventType = JsonHelper.GetValue<string>(eventData, "type");

                    if (eventType == "init")
                    {
                        string sessionId = JsonHelper.GetValue<string>(eventData, "session_id");
                        if (!string.IsNullOrEmpty(sessionId))
                            response.Id = sessionId;
                    }
                    else if (eventType == "message")
                    {
                        string role = JsonHelper.GetValue<string>(eventData, "role");
                        if (role == "assistant")
                        {
                            string content = JsonHelper.GetValue<string>(eventData, "content");
                            if (!string.IsNullOrEmpty(content))
                            {
                                if (contentBuilder.Length > 0)
                                    contentBuilder.AppendLine();
                                contentBuilder.Append(content);
                            }
                        }
                    }
                    else if (eventType == "result")
                    {
                        response.FinishReason = "stop";

                        string resultText = JsonHelper.GetValue<string>(eventData, "response");
                        if (!string.IsNullOrEmpty(resultText) && contentBuilder.Length == 0)
                        {
                            contentBuilder.Append(resultText);
                        }

                        Dictionary<string, object> stats = JsonHelper.GetObject(eventData, "stats");
                        if (stats != null)
                        {
                            int inputTokens = JsonHelper.GetValue<int>(stats, "input_tokens");
                            int outputTokens = JsonHelper.GetValue<int>(stats, "output_tokens");

                            if (inputTokens > 0 || outputTokens > 0)
                            {
                                response.Usage = new ChatCliResponseUsageInfo
                                {
                                    PromptTokens = inputTokens,
                                    CompletionTokens = outputTokens,
                                    TotalTokens = inputTokens + outputTokens
                                };
                            }
                        }
                    }
                }
                catch { }
            }

            response.Content = contentBuilder.ToString();

            if (string.IsNullOrEmpty(response.Content))
                return ChatCliResponse.FromError("Gemini CLI persistent response did not include content.");

            return response;
        }

        private void LogRequestError(string model_, string cliExecutable_, string cliArguments_, string promptText_, string errorMessage_)
        {
            string truncatedPrompt = GetTruncatedString(promptText_, 300);
            string truncatedArgs = GetTruncatedString(cliArguments_, 200);
            AIProviderLogger.LogError($"[Gemini CLI] Request failed - Model: {model_ ?? "(default)"}, CLI: {cliExecutable_ ?? "(unknown)"}, Args: {truncatedArgs}, Prompt: \"{truncatedPrompt}\", Error: {errorMessage_}");
        }

        private string GetTruncatedString(string value_, int maxLength_)
        {
            if (string.IsNullOrEmpty(value_))
                return "(null)";
            if (value_.Length > maxLength_)
                return value_.Substring(0, maxLength_) + $"...(length:{value_.Length})";
            return value_;
        }

        private string BuildPromptText(ChatCliRequestPayload requestPayload_)
        {
            System.Text.StringBuilder promptBuilder = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(requestPayload_.SystemPrompt))
            {
                promptBuilder.AppendLine($"System: {requestPayload_.SystemPrompt}");
                promptBuilder.AppendLine();
            }

            foreach (ChatCliRequestMessage message in requestPayload_.Messages)
            {
                if (message == null)
                    continue;

                string role = message.GetRoleString();
                string content = message.Content ?? string.Empty;

                promptBuilder.AppendLine($"{char.ToUpper(role[0]) + role.Substring(1)}: {content}");
            }

            return promptBuilder.ToString().Trim();
        }

        private Dictionary<string, string> BuildEnvironmentVariables()
        {
            Dictionary<string, string> environment = new Dictionary<string, string>();

            if (_settings.UseApiKey && !string.IsNullOrEmpty(_settings.ApiKey))
                environment[GeminiChatCliConfig.GEMINI_API_KEY_ENV] = _settings.ApiKey;

            if (_settings.CliEnvironmentVariables != null)
            {
                foreach (KeyValuePair<string, string> kvp in _settings.CliEnvironmentVariables)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                        environment[kvp.Key] = kvp.Value ?? string.Empty;
                }
            }

            return environment;
        }

        private string BuildCliArguments(string baseArguments_, string model_, string promptText_)
        {
            string arguments = baseArguments_ ?? string.Empty;
            arguments = EnsureOutputFormatArgument(arguments);
            arguments = EnsureModelArgument(arguments, model_);
            arguments = AppendPromptArgument(arguments, promptText_);
            return arguments.Trim();
        }

        private string EnsureOutputFormatArgument(string arguments_)
        {
            if (HasArgumentPrefix(arguments_, "--output-format"))
                return arguments_;

            string trimmed = arguments_.Trim();

            if (string.IsNullOrEmpty(trimmed))
                return "--output-format json";

            return $"{trimmed} --output-format json";
        }

        private string EnsureModelArgument(string arguments_, string model_)
        {
            if (string.IsNullOrEmpty(model_))
                return arguments_;

            if (HasArgumentToken(arguments_, "--model") || HasArgumentToken(arguments_, "-m") || HasArgumentPrefix(arguments_, "--model="))
                return arguments_;

            string trimmed = arguments_.Trim();
            string escapedModel = EscapeArgumentValue(model_);

            if (string.IsNullOrEmpty(trimmed))
                return $"--model \"{escapedModel}\"";

            return $"{trimmed} --model \"{escapedModel}\"";
        }

        private string AppendPromptArgument(string arguments_, string promptText_)
        {
            if (string.IsNullOrEmpty(promptText_))
                return arguments_;

            if (HasArgumentToken(arguments_, "-p") ||
                HasArgumentToken(arguments_, "--prompt") ||
                HasArgumentPrefix(arguments_, "--prompt="))
            {
                return arguments_;
            }

            string trimmed = arguments_.Trim();
            string escapedPrompt = EscapeArgumentValue(promptText_);

            if (string.IsNullOrEmpty(trimmed))
                return $"--prompt \"{escapedPrompt}\"";

            return $"{trimmed} --prompt \"{escapedPrompt}\"";
        }

        private bool HasArgumentToken(string arguments_, string token_)
        {
            if (string.IsNullOrEmpty(arguments_))
                return false;

            string[] tokens = arguments_.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                if (string.Equals(token, token_, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private bool HasArgumentPrefix(string arguments_, string prefix_)
        {
            if (string.IsNullOrEmpty(arguments_) || string.IsNullOrEmpty(prefix_))
                return false;

            string[] tokens = arguments_.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                if (token.StartsWith(prefix_, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private string EscapeArgumentValue(string value_)
        {
            if (string.IsNullOrEmpty(value_))
                return string.Empty;

            return value_.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private ChatCliResponse ParseGeminiResponse(string jsonOutput_, string model_)
        {
            if (string.IsNullOrEmpty(jsonOutput_))
                return ChatCliResponse.FromError("Gemini CLI response was empty.");

            ChatCliResponse response = new ChatCliResponse
            {
                Model = model_
            };

            try
            {
                Dictionary<string, object> responseData = JsonHelper.Deserialize(jsonOutput_);
                if (responseData == null)
                    return ChatCliResponse.FromError("Gemini CLI response could not be parsed.");

                Dictionary<string, object> errorData = JsonHelper.GetObject(responseData, "error");
                if (errorData != null)
                {
                    string errorMessage = JsonHelper.GetValue<string>(errorData, "message");
                    if (!string.IsNullOrEmpty(errorMessage))
                        return ChatCliResponse.FromError(errorMessage);
                }

                string resultText = JsonHelper.GetValue<string>(responseData, "response");
                if (!string.IsNullOrEmpty(resultText))
                {
                    response.Content = resultText;
                }

                response.FinishReason = "stop";
            }
            catch
            {
                response.Content = jsonOutput_;
            }

            if (string.IsNullOrEmpty(response.Content))
                return ChatCliResponse.FromError("Gemini CLI response did not include content.");

            return response;
        }

        private bool IsPlatformSupported()
        {
#if UNITY_EDITOR
            return Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.OSXEditor ||
                   Application.platform == RuntimePlatform.LinuxEditor;
#else
            return Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.LinuxPlayer;
#endif
        }

        protected override void DisposeInternal()
        {
            ((IDisposable)_cliWrapper)?.Dispose();
        }
    }
}