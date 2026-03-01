using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal class ChatCliProviderCodex : ChatCliProviderAbstract
    {
        private readonly CodexCliWrapper _cliWrapper;
        private string _sessionId;
        private bool _loggedPersistentUnsupported;

        internal override ChatCliProviderType ProviderType => ChatCliProviderType.CODEX_CLI;
        internal override bool SupportsPersistentSession => true;

        internal ChatCliProviderCodex(ChatCliProviderSettings settings_) : base(settings_)
        {
            _cliWrapper = new CodexCliWrapper(_settings.TimeoutSeconds);
        }

        internal override async Task<ChatCliResponse> SendMessageAsync(
            ChatCliRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return ChatCliResponse.FromError("Request cannot be null.");

            if (!IsPlatformSupported())
                return ChatCliResponse.FromError("Codex CLI is supported on Windows/macOS editors and desktop standalone platforms.");

            if (requestPayload_.Messages == null || requestPayload_.Messages.Count == 0)
                return ChatCliResponse.FromError("Messages cannot be empty.");

            try
            {
                string model = model_ ?? _settings.DefaultModel;
                if (model == CodexCliWrapper.AUTO_MODEL_ID)
                    model = string.Empty;
                
                string promptText = BuildPromptText(requestPayload_);

                string cliExecutable = string.IsNullOrEmpty(_settings.CliExecutablePath)
                    ? CodexChatCliConfig.DEFAULT_EXECUTABLE_NAME
                    : _settings.CliExecutablePath;

                string cliArgumentsBase = string.IsNullOrEmpty(_settings.CliArguments)
                    ? CodexChatCliConfig.DEFAULT_CHAT_ARGUMENTS
                    : _settings.CliArguments;

                
                string cliArguments = BuildCliArguments(cliArgumentsBase, model);

                Dictionary<string, string> environment = BuildEnvironmentVariables();

                CodexCliResponseResult result = await _cliWrapper.ExecuteAsync(
                        cliExecutable,
                        cliArguments,
                        promptText,
                        environment,
                        _settings.CliWorkingDirectory,
                        _settings.NodeExecutablePath,
                        cancellationToken_);

                if (!result.IsSuccess)
                {
                    string error = ExtractErrorFromJsonlOutput(result.StandardOutput)
                        ?? result.ErrorMessage ?? result.StandardError ?? "Codex CLI request failed.";
                    LogRequestError(model, cliExecutable, cliArguments, promptText, error);
                    return ChatCliResponse.FromError(error);
                }

                ChatCliResponse response = ParseCodexExecResponse(result.StandardOutput, model);
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
                return ChatCliResponse.FromError("Codex CLI is supported on Windows/macOS editors and desktop standalone platforms.");

            if (requestPayload_.Messages == null || requestPayload_.Messages.Count == 0)
                return ChatCliResponse.FromError("Messages cannot be empty.");

            try
            {
                string model = model_ ?? _settings.DefaultModel;
                if (model == CodexCliWrapper.AUTO_MODEL_ID)
                    model = string.Empty;

                string lastUserMessage = ExtractLastUserMessage(requestPayload_);
                if (string.IsNullOrEmpty(lastUserMessage))
                    return ChatCliResponse.FromError("No user message found in request.");

                string cliExecutable = string.IsNullOrEmpty(_settings.CliExecutablePath)
                    ? CodexChatCliConfig.DEFAULT_EXECUTABLE_NAME
                    : _settings.CliExecutablePath;

                string cliArguments = BuildPersistentCliArguments(model);

                Dictionary<string, string> environment = BuildEnvironmentVariables();

                CodexCliResponseResult result = await _cliWrapper.ExecutePersistentAsync(
                    cliExecutable,
                    cliArguments,
                    lastUserMessage,
                    environment,
                    _settings.CliWorkingDirectory,
                    _settings.NodeExecutablePath,
                    cancellationToken_);

                if (!result.IsSuccess)
                {
                    string error = ExtractErrorFromJsonlOutput(result.StandardOutput)
                        ?? result.ErrorMessage ?? result.StandardError ?? "Codex CLI persistent request failed.";
                    LogRequestError(model, cliExecutable, cliArguments, lastUserMessage, error);
                    return ChatCliResponse.FromError(error);
                }

                ChatCliResponse response = ParseCodexExecResponse(result.StandardOutput, model);
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
            string arguments = CodexChatCliConfig.DEFAULT_CHAT_ARGUMENTS;
            arguments = EnsureModelArgument(arguments, model_);
            arguments = EnsurePromptArgument(arguments);
            return arguments.Trim();
        }

        private string ExtractErrorFromJsonlOutput(string jsonlOutput_)
        {
            if (string.IsNullOrEmpty(jsonlOutput_))
                return null;

            string[] lines = jsonlOutput_.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    Dictionary<string, object> eventData = JsonHelper.Deserialize(line);
                    if (eventData == null)
                        continue;

                    string eventType = JsonHelper.GetValue<string>(eventData, "type");

                    if (eventType == "turn.failed")
                    {
                        Dictionary<string, object> error = JsonHelper.GetObject(eventData, "error");
                        if (error != null)
                        {
                            string message = JsonHelper.GetValue<string>(error, "message");
                            if (!string.IsNullOrEmpty(message))
                                return message;
                        }
                    }
                    else if (eventType == "error")
                    {
                        string message = JsonHelper.GetValue<string>(eventData, "message");
                        if (!string.IsNullOrEmpty(message))
                            return message;
                    }
                }
                catch { }
            }

            return null;
        }

        private void LogRequestError(string model_, string cliExecutable_, string cliArguments_, string promptText_, string errorMessage_)
        {
            string truncatedPrompt = GetTruncatedString(promptText_, 300);
            string truncatedArgs = GetTruncatedString(cliArguments_, 200);
            AIProviderLogger.LogError($"[Codex CLI] Request failed - Model: {model_ ?? "(default)"}, CLI: {cliExecutable_ ?? "(unknown)"}, Args: {truncatedArgs}, Prompt: \"{truncatedPrompt}\", Error: {errorMessage_}");
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
                environment[CodexChatCliConfig.OPENAI_API_KEY_ENV] = _settings.ApiKey;

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

        private string BuildCliArguments(string baseArguments_, string model_)
        {
            string arguments = baseArguments_ ?? string.Empty;
            arguments = EnsureModelArgument(arguments, model_);
            arguments = EnsurePromptArgument(arguments);
            return arguments.Trim();
        }

        private string EnsureModelArgument(string arguments_, string model_)
        {
            if (string.IsNullOrEmpty(model_))
                return arguments_;

            if (HasArgumentPrefix(arguments_, "-m") || HasArgumentPrefix(arguments_, "--model"))
                return arguments_;

            string trimmed = arguments_.Trim();

            if (string.IsNullOrEmpty(trimmed))
                return $"-m {model_}";

            return $"{trimmed} -m {model_}";
        }

        private string EnsurePromptArgument(string arguments_)
        {
            if (HasArgumentToken(arguments_, "-"))
                return arguments_;

            string trimmed = arguments_.Trim();

            if (string.IsNullOrEmpty(trimmed))
                return "-";

            return $"{trimmed} -";
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

            return value_.Replace("\"", "\\\"");
        }

        private ChatCliResponse ParseCodexExecResponse(string jsonlOutput_, string model_)
        {
            if (string.IsNullOrEmpty(jsonlOutput_))
                return ChatCliResponse.FromError("Codex CLI response was empty.");

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

                    if (eventType == "thread.started")
                    {
                        response.Id = JsonHelper.GetValue<string>(eventData, "thread_id");
                    }
                    else if (eventType == "item.completed")
                    {
                        Dictionary<string, object> item = JsonHelper.GetObject(eventData, "item");
                        if (item != null)
                        {
                            string itemType = JsonHelper.GetValue<string>(item, "type");
                            if (itemType == "agent_message")
                            {
                                string text = JsonHelper.GetValue<string>(item, "text");
                                if (!string.IsNullOrEmpty(text))
                                {
                                    if (contentBuilder.Length > 0)
                                        contentBuilder.AppendLine();
                                    contentBuilder.Append(text);
                                }
                            }
                        }
                    }
                    else if (eventType == "turn.completed")
                    {
                        response.FinishReason = "stop";
                        Dictionary<string, object> usage = JsonHelper.GetObject(eventData, "usage");
                        if (usage != null)
                        {
                            int inputTokens = JsonHelper.GetValue<int>(usage, "input_tokens");
                            int outputTokens = JsonHelper.GetValue<int>(usage, "output_tokens");

                            response.Usage = new ChatCliResponseUsageInfo
                            {
                                PromptTokens = inputTokens,
                                CompletionTokens = outputTokens,
                                TotalTokens = inputTokens + outputTokens
                            };
                        }
                    }
                }
                catch { }
            }

            response.Content = contentBuilder.ToString();

            if (string.IsNullOrEmpty(response.Content))
                return ChatCliResponse.FromError("Codex CLI response did not include content.");

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
