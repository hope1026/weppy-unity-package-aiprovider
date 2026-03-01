using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    internal class CodexAppServerClient : IDisposable
    {
        private const string DEFAULT_EXECUTABLE = "codex";
        private const string DEFAULT_ARGUMENTS = "app-server --listen stdio://";
        private const string OPENAI_API_KEY_ENV = "OPENAI_API_KEY";
        private static readonly string[] IMAGE_EXTENSIONS = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };

        private readonly int _timeoutSeconds;
        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);
        private readonly object _stderrLock = new object();
        private readonly List<string> _stderrLines = new List<string>();
        private Process _process;
        private StreamWriter _standardInput;
        private StreamReader _standardOutput;
        private Task _stderrReaderTask;
        private string _processSignature;
        private bool _initialized;
        private string _threadId;
        private string _threadModel;
        private int _requestId;
        private bool _disposed;

        internal CodexAppServerClient(int timeoutSeconds_ = 180)
        {
            _timeoutSeconds = timeoutSeconds_;
        }

        internal async Task<CodexAppImageResult> GenerateImageAsync(
            ImageRequestPayload requestPayload_,
            string model_,
            ImageAppProviderSettings settings_,
            CancellationToken cancellationToken_)
        {
            await _requestLock.WaitAsync(cancellationToken_);
            int stderrStartIndex = 0;
            try
            {
                if (!EnsureProcess(settings_))
                {
                    return CodexAppImageResult.FromError("Failed to start Codex app-server process.");
                }

                stderrStartIndex = GetStderrCount();
                string requestedModel = string.IsNullOrWhiteSpace(model_) ? settings_?.DefaultModel : model_;
                bool useThreadScopedModel = settings_ != null &&
                                            settings_.UseApiKey &&
                                            !string.IsNullOrWhiteSpace(requestedModel);
                string threadScopedModel = useThreadScopedModel ? requestedModel : string.Empty;

                // Codex app-server model is thread-scoped. If model changes, create a new thread.
                if (!string.IsNullOrEmpty(_threadId) &&
                    !string.Equals(_threadModel ?? string.Empty, threadScopedModel ?? string.Empty, StringComparison.Ordinal))
                {
                    _threadId = null;
                    _threadModel = null;
                }

                if (!_initialized)
                {
                    CodexAppJsonRpcResult initResult = await SendRequestAsync(
                        "initialize",
                        new Dictionary<string, object>
                        {
                            ["clientInfo"] = new Dictionary<string, object>
                            {
                                ["name"] = "weppy-aiprovider",
                                ["version"] = "1.0.0"
                            },
                            ["capabilities"] = null
                        },
                        cancellationToken_);

                    if (!initResult.IsSuccess)
                        return CodexAppImageResult.FromError(WithStderr(initResult.ErrorMessage, stderrStartIndex));

                    _initialized = true;
                }

                if (string.IsNullOrEmpty(_threadId))
                {
                    Dictionary<string, object> threadStartParams = new Dictionary<string, object>
                    {
                        ["source"] = "sdk"
                    };

                    // 'codex-app-image' model is available only when using API-key mode.
                    if (useThreadScopedModel)
                        threadStartParams["model"] = requestedModel;

                    CodexAppJsonRpcResult threadResult = await SendRequestAsync(
                        "thread/start",
                        threadStartParams,
                        cancellationToken_);

                    if (!threadResult.IsSuccess)
                        return CodexAppImageResult.FromError(WithStderr(threadResult.ErrorMessage, stderrStartIndex));

                    Dictionary<string, object> thread = JsonHelper.GetObject(threadResult.Result, "thread");
                    _threadId = JsonHelper.GetValue<string>(thread, "id");
                    if (string.IsNullOrEmpty(_threadId))
                        return CodexAppImageResult.FromError(WithStderr("Codex app-server did not return a thread id.", stderrStartIndex));

                    _threadModel = threadScopedModel ?? string.Empty;
                }

                string prompt = BuildPromptText(requestPayload_, useThreadScopedModel ? requestedModel : null);
                ImageArtifactCollector collector = new ImageArtifactCollector();
                CodexAppTurnResult turnResult = await StartTurnAndCollectAsync(prompt, collector, cancellationToken_);
                if (!turnResult.IsSuccess)
                    return CodexAppImageResult.FromError(WithStderr(turnResult.ErrorMessage, stderrStartIndex));

                return collector.ToResult(turnResult.AgentText, WithStderr(null, stderrStartIndex));
            }
            catch (OperationCanceledException)
            {
                return CodexAppImageResult.FromError("Request was cancelled.");
            }
            catch (TimeoutException ex)
            {
                return CodexAppImageResult.FromError(WithStderr(ex.Message, stderrStartIndex));
            }
            catch (Exception ex)
            {
                return CodexAppImageResult.FromError(WithStderr(ex.Message, stderrStartIndex));
            }
            finally
            {
                _requestLock.Release();
            }
        }

        private string BuildPromptText(ImageRequestPayload requestPayload_, string model_)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Generate an image and provide machine-readable output.");
            if (!string.IsNullOrEmpty(model_))
                sb.AppendLine($"Preferred model: {model_}");
            sb.AppendLine($"Prompt: {requestPayload_.Prompt}");
            if (!string.IsNullOrWhiteSpace(requestPayload_.NegativePrompt))
                sb.AppendLine($"Negative prompt: {requestPayload_.NegativePrompt}");
            sb.AppendLine($"Number of images: {Math.Max(1, requestPayload_.NumberOfImages)}");
            sb.AppendLine();
            sb.AppendLine("Return ONLY valid JSON in this schema:");
            sb.AppendLine("{\"images\":[{\"path\":\"/absolute/path.png\",\"url\":\"https://...\",\"b64\":\"...\"}],\"notes\":\"...\"}");
            sb.AppendLine("At least one image item must include path or url or b64.");
            sb.AppendLine("Do not include markdown code fences.");
            return sb.ToString().Trim();
        }

        private async Task<CodexAppTurnResult> StartTurnAndCollectAsync(
            string prompt_,
            ImageArtifactCollector collector_,
            CancellationToken cancellationToken_)
        {
            string requestId = NextRequestId();
            Dictionary<string, object> request = new Dictionary<string, object>
            {
                ["method"] = "turn/start",
                ["id"] = requestId,
                ["params"] = new Dictionary<string, object>
                {
                    ["threadId"] = _threadId,
                    ["input"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "text",
                            ["text"] = prompt_,
                            ["text_elements"] = new List<object>()
                        }
                    }
                }
            };

            await WriteRequestAsync(request, cancellationToken_);

            string turnId = null;
            StringBuilder agentText = new StringBuilder();
            DateTime startedAt = DateTime.UtcNow;
            bool receivedTurnStartResponse = false;

            while (true)
            {
                string line = await ReadLineAsync(startedAt, cancellationToken_);
                if (line == null)
                    return CodexAppTurnResult.FromError("Codex app-server terminated unexpectedly.");

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Dictionary<string, object> message = null;
                try
                {
                    message = JsonHelper.Deserialize(line);
                }
                catch
                {
                }

                if (message == null)
                    continue;

                string responseId = JsonHelper.GetValue<string>(message, "id");
                string method = JsonHelper.GetValue<string>(message, "method");
                bool isResponseMessage = IsResponseMessage(message);
                bool isServerRequestMessage = !string.IsNullOrEmpty(method) && !isResponseMessage;

                // Server-originated request: method + id + no result/error.
                if (isServerRequestMessage && !string.IsNullOrEmpty(responseId))
                {
                    await HandleServerRequestAsync(method, responseId, message, cancellationToken_);
                    continue;
                }

                // Response for turn/start.
                if (isResponseMessage && responseId == requestId)
                {
                    Dictionary<string, object> error = JsonHelper.GetObject(message, "error");
                    if (error != null)
                    {
                        return CodexAppTurnResult.FromError(JsonHelper.GetValue<string>(error, "message") ?? "turn/start failed.");
                    }

                    Dictionary<string, object> result = JsonHelper.GetObject(message, "result");
                    if (result != null)
                    {
                        Dictionary<string, object> turn = JsonHelper.GetObject(result, "turn");
                        turnId = JsonHelper.GetValue<string>(turn, "id");
                        receivedTurnStartResponse = true;
                    }

                    continue;
                }

                // Ignore unrelated responses.
                if (isResponseMessage && responseId != requestId)
                    continue;

                if (string.IsNullOrEmpty(method))
                    continue;

                // Any output line indicates the app-server is still active.
                startedAt = DateTime.UtcNow;

                string normalizedMethod = NormalizeMethod(method);
                Dictionary<string, object> @params = JsonHelper.GetObject(message, "params");
                if (@params == null)
                    continue;

                if (normalizedMethod == "item/completed")
                {
                    Dictionary<string, object> item = JsonHelper.GetObject(@params, "item");
                    if (item != null)
                    {
                        string itemType = JsonHelper.GetValue<string>(item, "type");
                        if (string.Equals(itemType, "agentMessage", StringComparison.OrdinalIgnoreCase))
                        {
                            string text = JsonHelper.GetValue<string>(item, "text");
                            if (!string.IsNullOrEmpty(text))
                            {
                                if (agentText.Length > 0)
                                    agentText.AppendLine();
                                agentText.Append(text);
                                collector_.TryExtractFromJsonText(text);
                            }
                        }
                        else if (string.Equals(itemType, "imageView", StringComparison.OrdinalIgnoreCase))
                        {
                            string path = JsonHelper.GetValue<string>(item, "path");
                            collector_.AddPath(path);
                        }

                        collector_.CollectCandidates(item);
                    }
                }
                else if (normalizedMethod == "rawResponseItem/completed")
                {
                    Dictionary<string, object> item = JsonHelper.GetObject(@params, "item");
                    if (item != null)
                    {
                        collector_.CollectCandidates(item);
                    }
                }
                else if (normalizedMethod == "error")
                {
                    Dictionary<string, object> error = JsonHelper.GetObject(@params, "error");
                    bool willRetry = JsonHelper.GetValue<bool>(@params, "willRetry");
                    string errorTurnId = JsonHelper.GetValue<string>(@params, "turnId");
                    if (!willRetry && (string.IsNullOrEmpty(turnId) || string.Equals(errorTurnId, turnId, StringComparison.Ordinal)))
                    {
                        return CodexAppTurnResult.FromError(JsonHelper.GetValue<string>(error, "message") ?? "Codex app-server turn failed.");
                    }
                }
                else if (normalizedMethod == "turn/completed")
                {
                    Dictionary<string, object> turn = JsonHelper.GetObject(@params, "turn");
                    string completedTurnId = JsonHelper.GetValue<string>(turn, "id");
                    string status = JsonHelper.GetValue<string>(turn, "status");
                    bool turnMatches = string.IsNullOrEmpty(turnId) || string.Equals(turnId, completedTurnId, StringComparison.Ordinal);
                    if (turnMatches)
                    {
                        if (string.Equals(status, "inProgress", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
                        {
                            Dictionary<string, object> turnError = JsonHelper.GetObject(turn, "error");
                            string errorMessage = turnError != null
                                ? JsonHelper.GetValue<string>(turnError, "message")
                                : "Codex app-server turn completed with a failure status.";
                            return CodexAppTurnResult.FromError(errorMessage);
                        }

                        if (!receivedTurnStartResponse)
                            return CodexAppTurnResult.FromError("turn/start response was not received.");

                        return CodexAppTurnResult.FromSuccess(agentText.ToString());
                    }
                }
            }
        }

        private async Task HandleServerRequestAsync(
            string method_,
            string requestId_,
            Dictionary<string, object> request_,
            CancellationToken cancellationToken_)
        {
            Dictionary<string, object> response = new Dictionary<string, object>
            {
                ["id"] = requestId_
            };

            switch (method_)
            {
                case "item/commandExecution/requestApproval":
                {
                    response["result"] = new Dictionary<string, object> { ["decision"] = "decline" };
                    break;
                }
                case "item/fileChange/requestApproval":
                {
                    response["result"] = new Dictionary<string, object> { ["decision"] = "decline" };
                    break;
                }
                case "item/tool/requestUserInput":
                {
                    response["result"] = new Dictionary<string, object>
                    {
                        ["answers"] = new Dictionary<string, object>()
                    };
                    break;
                }
                case "item/tool/call":
                {
                    response["result"] = new Dictionary<string, object>
                    {
                        ["contentItems"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["type"] = "inputText",
                                ["text"] = "Dynamic tool calls are not supported by ImageAppProviderCodex."
                            }
                        },
                        ["success"] = false
                    };
                    break;
                }
                case "applyPatchApproval":
                case "execCommandApproval":
                {
                    response["result"] = new Dictionary<string, object> { ["decision"] = "denied" };
                    break;
                }
                default:
                {
                    response["result"] = new Dictionary<string, object>();
                    break;
                }
            }

            await WriteRequestAsync(response, cancellationToken_);
        }

        private async Task<CodexAppJsonRpcResult> SendRequestAsync(
            string method_,
            Dictionary<string, object> params_,
            CancellationToken cancellationToken_)
        {
            string requestId = NextRequestId();
            Dictionary<string, object> request = new Dictionary<string, object>
            {
                ["method"] = method_,
                ["id"] = requestId,
                ["params"] = params_
            };

            await WriteRequestAsync(request, cancellationToken_);

            DateTime startedAt = DateTime.UtcNow;
            try
            {
                while (true)
                {
                    string line = await ReadLineAsync(startedAt, cancellationToken_);
                    if (line == null)
                        return CodexAppJsonRpcResult.FromError("Codex app-server terminated unexpectedly.");

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    Dictionary<string, object> message = null;
                    try
                    {
                        message = JsonHelper.Deserialize(line);
                    }
                    catch
                    {
                    }

                    if (message == null)
                        continue;

                    string responseId = JsonHelper.GetValue<string>(message, "id");
                    string method = JsonHelper.GetValue<string>(message, "method");
                    bool isResponseMessage = IsResponseMessage(message);
                    bool isServerRequestMessage = !string.IsNullOrEmpty(method) && !isResponseMessage;

                    if (isServerRequestMessage && !string.IsNullOrEmpty(responseId))
                    {
                        await HandleServerRequestAsync(method, responseId, message, cancellationToken_);
                        continue;
                    }

                    if (!isResponseMessage || responseId != requestId)
                        continue;

                    Dictionary<string, object> error = JsonHelper.GetObject(message, "error");
                    if (error != null)
                    {
                        return CodexAppJsonRpcResult.FromError(JsonHelper.GetValue<string>(error, "message") ?? $"{method_} failed.");
                    }

                    Dictionary<string, object> result = JsonHelper.GetObject(message, "result");
                    return CodexAppJsonRpcResult.FromSuccess(result);
                }
            }
            catch (TimeoutException)
            {
                return CodexAppJsonRpcResult.FromError($"{method_} request timed out.");
            }
        }

        private async Task WriteRequestAsync(Dictionary<string, object> request_, CancellationToken cancellationToken_)
        {
            if (request_ != null && !request_.ContainsKey("jsonrpc"))
                request_["jsonrpc"] = "2.0";

            string line = JsonHelper.Serialize(request_);
            await _standardInput.WriteLineAsync(line);
            await _standardInput.FlushAsync();
            cancellationToken_.ThrowIfCancellationRequested();
        }

        private bool IsResponseMessage(Dictionary<string, object> message_)
        {
            if (message_ == null)
                return false;

            return message_.ContainsKey("result") || message_.ContainsKey("error");
        }

        private async Task<string> ReadLineAsync(DateTime startedAt_, CancellationToken cancellationToken_)
        {
            if (_standardOutput == null)
                return null;

            while (true)
            {
                Task<string> readTask = _standardOutput.ReadLineAsync();
                Task finishedTask;
                if (_timeoutSeconds > 0)
                {
                    TimeSpan elapsed = DateTime.UtcNow - startedAt_;
                    TimeSpan remaining = TimeSpan.FromSeconds(_timeoutSeconds) - elapsed;
                    if (remaining <= TimeSpan.Zero)
                        throw new TimeoutException("Codex app-server request timed out.");

                    Task delayTask = Task.Delay(remaining, cancellationToken_);
                    finishedTask = await Task.WhenAny(readTask, delayTask);
                    if (finishedTask != readTask)
                    {
                        if (cancellationToken_.IsCancellationRequested)
                            throw new OperationCanceledException(cancellationToken_);
                        throw new TimeoutException("Codex app-server request timed out.");
                    }
                }
                else
                {
                    finishedTask = await Task.WhenAny(readTask, Task.Delay(Timeout.Infinite, cancellationToken_));
                    if (finishedTask != readTask)
                        throw new OperationCanceledException(cancellationToken_);
                }

                return await readTask;
            }
        }

        private bool EnsureProcess(ImageAppProviderSettings settings_)
        {
            string signature = BuildSignature(settings_);
            if (_process != null && !_process.HasExited && string.Equals(_processSignature, signature, StringComparison.Ordinal))
                return true;

            ResetProcess();

            string executable = ResolveAppServerExecutablePath(settings_?.AppExecutablePath);
            string arguments = string.IsNullOrWhiteSpace(settings_?.AppArguments)
                ? DEFAULT_ARGUMENTS
                : settings_.AppArguments;
            string workingDirectory = settings_?.AppWorkingDirectory;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
                startInfo.WorkingDirectory = workingDirectory;

            EnsurePathEnvironmentVariable(startInfo, settings_?.NodeExecutablePath);

            if (settings_ != null)
            {
                if (settings_.UseApiKey && !string.IsNullOrEmpty(settings_.ApiKey))
                    startInfo.EnvironmentVariables[OPENAI_API_KEY_ENV] = settings_.ApiKey;

                if (settings_.AppEnvironmentVariables != null)
                {
                    foreach (KeyValuePair<string, string> env in settings_.AppEnvironmentVariables)
                    {
                        if (!string.IsNullOrEmpty(env.Key))
                            startInfo.EnvironmentVariables[env.Key] = env.Value ?? string.Empty;
                    }
                }
            }

            Process process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                process.Dispose();
                return false;
            }

            _process = process;
            _standardInput = process.StandardInput;
            _standardOutput = process.StandardOutput;
            _processSignature = signature;
            _initialized = false;
            _threadId = null;
            ResetStderrBuffer();
            StartStderrReader(process);
            return true;
        }

        private string BuildSignature(ImageAppProviderSettings settings_)
        {
            string executable = ResolveAppServerExecutablePath(settings_?.AppExecutablePath);
            string arguments = string.IsNullOrWhiteSpace(settings_?.AppArguments)
                ? DEFAULT_ARGUMENTS
                : settings_.AppArguments;
            string workingDirectory = settings_?.AppWorkingDirectory ?? string.Empty;
            string nodeExecutablePath = settings_?.NodeExecutablePath ?? string.Empty;
            return $"{executable}\n{arguments}\n{workingDirectory}\n{nodeExecutablePath}";
        }

        private string ResolveAppServerExecutablePath(string configuredExecutablePath_)
        {
            if (string.IsNullOrWhiteSpace(configuredExecutablePath_))
                return DEFAULT_EXECUTABLE;

            string trimmed = configuredExecutablePath_.Trim();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (trimmed.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                string appBundlePath = trimmed.TrimEnd('/', '\\');
                string embeddedCliPath = Path.Combine(appBundlePath, "Contents", "Resources", "codex");
                if (File.Exists(embeddedCliPath))
                    return embeddedCliPath;
            }

            const string macGuiSuffix = "/Contents/MacOS/Codex";
            int guiSuffixIndex = trimmed.LastIndexOf(macGuiSuffix, StringComparison.OrdinalIgnoreCase);
            if (guiSuffixIndex >= 0)
            {
                string appRoot = trimmed.Substring(0, guiSuffixIndex);
                string embeddedCliPath = Path.Combine(appRoot, "Contents", "Resources", "codex");
                if (File.Exists(embeddedCliPath))
                    return embeddedCliPath;
            }
#endif

            return trimmed;
        }

        private void EnsurePathEnvironmentVariable(ProcessStartInfo startInfo_, string nodeExecutablePath_)
        {
            if (startInfo_ == null)
                return;

            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            List<string> additionalPaths = new List<string>();

            if (!string.IsNullOrEmpty(nodeExecutablePath_))
            {
                string nodeDir = Path.GetDirectoryName(nodeExecutablePath_);
                if (!string.IsNullOrEmpty(nodeDir))
                {
                    additionalPaths.Add(nodeDir);
                }
            }
            else
            {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                additionalPaths.Add("/opt/homebrew/bin");
                additionalPaths.Add("/usr/local/bin");
                additionalPaths.Add("/usr/bin");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    additionalPaths.Add($"{homeDir}/.local/bin");
                    additionalPaths.Add($"{homeDir}/.nvm/versions/node/latest/bin");
                    additionalPaths.Add($"{homeDir}/.bun/bin");
                }
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                additionalPaths.Add("/usr/local/bin");
                additionalPaths.Add("/usr/bin");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    additionalPaths.Add($"{homeDir}/.local/bin");
                    additionalPaths.Add($"{homeDir}/.nvm/versions/node/latest/bin");
                    additionalPaths.Add($"{homeDir}/.bun/bin");
                }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (!string.IsNullOrEmpty(programFiles))
                {
                    additionalPaths.Add($"{programFiles}\\nodejs");
                }
                if (!string.IsNullOrEmpty(programFilesX86))
                {
                    additionalPaths.Add($"{programFilesX86}\\nodejs");
                }
#endif
            }

            string pathSeparator = Path.PathSeparator.ToString();
            List<string> existingPathParts =
                new List<string>(currentPath.Split(new[] { pathSeparator }, StringSplitOptions.RemoveEmptyEntries));

            foreach (string additionalPath in additionalPaths)
            {
                if (!string.IsNullOrEmpty(additionalPath) && !existingPathParts.Contains(additionalPath))
                {
                    existingPathParts.Insert(0, additionalPath);
                }
            }

            string enhancedPath = string.Join(pathSeparator, existingPathParts);
            startInfo_.EnvironmentVariables["PATH"] = enhancedPath;
        }

        private void ResetProcess()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                    _process.Kill();
            }
            catch
            {
            }

            try { _standardInput?.Dispose(); } catch { }
            try { _standardOutput?.Dispose(); } catch { }
            try { _process?.Dispose(); } catch { }

            _standardInput = null;
            _standardOutput = null;
            _process = null;
            _processSignature = null;
            _initialized = false;
            _threadId = null;
            _threadModel = null;
        }

        private void StartStderrReader(Process process_)
        {
            _stderrReaderTask = Task.Run(async () =>
            {
                try
                {
                    while (process_ != null && !process_.HasExited)
                    {
                        string line = await process_.StandardError.ReadLineAsync();
                        if (line == null)
                            break;

                        lock (_stderrLock)
                        {
                            _stderrLines.Add(line);
                            if (_stderrLines.Count > 200)
                                _stderrLines.RemoveAt(0);
                        }
                    }
                }
                catch
                {
                }
            });
        }

        private void ResetStderrBuffer()
        {
            lock (_stderrLock)
            {
                _stderrLines.Clear();
            }
        }

        private int GetStderrCount()
        {
            lock (_stderrLock)
            {
                return _stderrLines.Count;
            }
        }

        private string GetStderrSince(int startIndex_)
        {
            lock (_stderrLock)
            {
                if (_stderrLines.Count <= startIndex_)
                    return string.Empty;

                StringBuilder sb = new StringBuilder();
                for (int i = startIndex_; i < _stderrLines.Count; i++)
                {
                    if (sb.Length > 0)
                        sb.AppendLine();
                    sb.Append(_stderrLines[i]);
                }

                return sb.ToString();
            }
        }

        private string WithStderr(string message_, int stderrStartIndex_)
        {
            string stderr = GetStderrSince(stderrStartIndex_);
            if (string.IsNullOrEmpty(stderr))
                return message_ ?? "Codex app-server request failed.";

            if (string.IsNullOrEmpty(message_))
                return stderr;

            return $"{message_} | stderr: {stderr}";
        }

        private string NormalizeMethod(string method_)
        {
            if (string.IsNullOrEmpty(method_))
                return string.Empty;

            if (method_.StartsWith("codex/event/", StringComparison.Ordinal))
                return method_.Substring("codex/event/".Length).Replace('_', '/');

            return method_;
        }

        private string NextRequestId()
        {
            _requestId++;
            return _requestId.ToString();
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
                return;

            ResetProcess();
            _requestLock.Dispose();
            _disposed = true;
        }

        private class CodexAppJsonRpcResult
        {
            internal bool IsSuccess { get; private set; }
            internal Dictionary<string, object> Result { get; private set; }
            internal string ErrorMessage { get; private set; }

            internal static CodexAppJsonRpcResult FromSuccess(Dictionary<string, object> result_)
            {
                return new CodexAppJsonRpcResult { IsSuccess = true, Result = result_ };
            }

            internal static CodexAppJsonRpcResult FromError(string errorMessage_)
            {
                return new CodexAppJsonRpcResult { IsSuccess = false, ErrorMessage = errorMessage_ };
            }
        }

        private class CodexAppTurnResult
        {
            internal bool IsSuccess { get; private set; }
            internal string AgentText { get; private set; }
            internal string ErrorMessage { get; private set; }

            internal static CodexAppTurnResult FromSuccess(string agentText_)
            {
                return new CodexAppTurnResult { IsSuccess = true, AgentText = agentText_ ?? string.Empty };
            }

            internal static CodexAppTurnResult FromError(string errorMessage_)
            {
                return new CodexAppTurnResult { IsSuccess = false, ErrorMessage = errorMessage_ };
            }
        }

        private class ImageArtifactCollector
        {
            private readonly HashSet<string> _paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _urls = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _base64Values = new HashSet<string>(StringComparer.Ordinal);

            internal void AddPath(string path_)
            {
                if (string.IsNullOrWhiteSpace(path_))
                    return;

                string trimmed = path_.Trim();
                if (trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed.Substring("file://".Length);

                _paths.Add(trimmed);
            }

            internal void AddUrl(string url_)
            {
                if (string.IsNullOrWhiteSpace(url_))
                    return;

                _urls.Add(url_.Trim());
            }

            internal void AddBase64(string base64_)
            {
                if (string.IsNullOrWhiteSpace(base64_))
                    return;

                string trimmed = base64_.Trim();
                if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    int commaIndex = trimmed.IndexOf(',');
                    if (commaIndex >= 0 && commaIndex + 1 < trimmed.Length)
                        trimmed = trimmed.Substring(commaIndex + 1);
                }

                _base64Values.Add(trimmed);
            }

            internal void CollectCandidates(object value_)
            {
                if (value_ == null)
                    return;

                if (value_ is Dictionary<string, object> dict)
                {
                    foreach (KeyValuePair<string, object> kvp in dict)
                    {
                        string key = kvp.Key ?? string.Empty;
                        if (kvp.Value is string str)
                        {
                            CollectString(key, str);
                        }
                        else
                        {
                            CollectCandidates(kvp.Value);
                        }
                    }

                    return;
                }

                if (value_ is List<object> list)
                {
                    foreach (object item in list)
                        CollectCandidates(item);
                    return;
                }

                if (value_ is string stringValue)
                {
                    CollectString(string.Empty, stringValue);
                }
            }

            internal void TryExtractFromJsonText(string text_)
            {
                if (string.IsNullOrWhiteSpace(text_))
                    return;

                int start = text_.IndexOf('{');
                int end = text_.LastIndexOf('}');
                if (start < 0 || end <= start)
                    return;

                string json = text_.Substring(start, end - start + 1);
                try
                {
                    Dictionary<string, object> parsed = JsonHelper.Deserialize(json);
                    CollectCandidates(parsed);
                }
                catch
                {
                }
            }

            internal CodexAppImageResult ToResult(string agentText_, string fallbackError_)
            {
                CodexAppImageResult result = new CodexAppImageResult
                {
                    AgentText = agentText_ ?? string.Empty
                };

                foreach (string base64 in _base64Values)
                    result.Base64Images.Add(base64);

                foreach (string url in _urls)
                {
                    if (url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Base64Images.Add(ExtractDataUriBase64(url));
                    }
                    else
                    {
                        result.ImageUrls.Add(url);
                    }
                }

                foreach (string path in _paths)
                {
                    if (File.Exists(path))
                    {
                        result.ImagePaths.Add(path);
                    }
                }

                if (result.ImagePaths.Count > 0 || result.ImageUrls.Count > 0 || result.Base64Images.Count > 0)
                {
                    result.IsSuccess = true;
                    return result;
                }

                result.IsSuccess = false;
                result.ErrorMessage = !string.IsNullOrEmpty(fallbackError_)
                    ? fallbackError_
                    : "Codex App did not return image artifacts (path/url/base64).";
                return result;
            }

            private void CollectString(string key_, string value_)
            {
                if (string.IsNullOrWhiteSpace(value_))
                    return;

                string trimmed = value_.Trim();
                string key = key_ ?? string.Empty;
                string loweredKey = key.ToLowerInvariant();

                if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    AddBase64(trimmed);
                    return;
                }

                if (loweredKey.Contains("b64") || loweredKey.Contains("base64"))
                {
                    AddBase64(trimmed);
                    return;
                }

                if (LooksLikeAbsoluteImagePath(trimmed))
                {
                    AddPath(trimmed);
                    return;
                }

                if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    if (trimmed.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                        AddPath(trimmed);
                    else
                        AddUrl(trimmed);
                    return;
                }

                MatchCollection matches = Regex.Matches(trimmed, @"(?<p>(?:[A-Za-z]:\\|/)[^""'\s]+?\.(?:png|jpg|jpeg|webp|gif))", RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Success)
                        AddPath(match.Groups["p"].Value);
                }
            }

            private bool LooksLikeAbsoluteImagePath(string value_)
            {
                if (string.IsNullOrWhiteSpace(value_))
                    return false;

                bool hasExtension = false;
                foreach (string extension in IMAGE_EXTENSIONS)
                {
                    if (value_.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        hasExtension = true;
                        break;
                    }
                }

                if (!hasExtension)
                    return false;

                return Path.IsPathRooted(value_);
            }

            private string ExtractDataUriBase64(string dataUri_)
            {
                int commaIndex = dataUri_.IndexOf(',');
                if (commaIndex < 0 || commaIndex + 1 >= dataUri_.Length)
                    return dataUri_;
                return dataUri_.Substring(commaIndex + 1);
            }
        }
    }

    internal class CodexAppImageResult
    {
        internal bool IsSuccess { get; set; }
        internal string ErrorMessage { get; set; }
        internal string AgentText { get; set; }
        internal List<string> ImagePaths { get; } = new List<string>();
        internal List<string> ImageUrls { get; } = new List<string>();
        internal List<string> Base64Images { get; } = new List<string>();

        internal static CodexAppImageResult FromError(string errorMessage_)
        {
            return new CodexAppImageResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage_
            };
        }
    }
}
