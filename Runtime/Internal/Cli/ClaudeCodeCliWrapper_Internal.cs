using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider.Chat
{
    public partial class ClaudeCodeCliWrapper : IDisposable
    {
        private readonly int _timeoutSeconds;
        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);
        private bool _disposed;
        private Process _persistentProcess;
        private StreamWriter _persistentInput;
        private StreamReader _persistentOutput;
        private string _persistentSignature;
        private bool _processReady;
        private string _initLine;
        private Task _stderrReaderTask;
        private readonly List<string> _stderrLines = new List<string>();
        private readonly object _stderrLock = new object();

        internal ClaudeCodeCliWrapper(int timeoutSeconds_ = 120)
        {
            _timeoutSeconds = timeoutSeconds_;
        }

        internal async Task<ClaudeCodeCliResponseResult> ExecuteAsync(
            string executablePath_,
            string arguments_,
            string standardInput_,
            Dictionary<string, string> environmentVariables_ = null,
            string workingDirectory_ = null,
            CancellationToken cancellationToken_ = default)
        {
            if (string.IsNullOrEmpty(executablePath_))
            {
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = "Claude Code CLI executable path is empty."
                };
            }

            string resolvedPath = ResolveExecutablePath(executablePath_);
            if (string.IsNullOrEmpty(resolvedPath))
            {
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = $"Claude Code CLI executable '{executablePath_}' not found in PATH or specified location."
                };
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = resolvedPath,
                    Arguments = arguments_ ?? string.Empty,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardInputEncoding = new UTF8Encoding(false),
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                if (!string.IsNullOrEmpty(workingDirectory_))
                    startInfo.WorkingDirectory = workingDirectory_;

                EnsurePathEnvironmentVariable(startInfo);

                if (environmentVariables_ != null)
                {
                    foreach (KeyValuePair<string, string> kvp in environmentVariables_)
                    {
                        if (!string.IsNullOrEmpty(kvp.Key))
                            startInfo.EnvironmentVariables[kvp.Key] = kvp.Value ?? string.Empty;
                    }
                }

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    if (!process.Start())
                    {
                        return new ClaudeCodeCliResponseResult
                        {
                            IsSuccess = false,
                            ExitCode = -1,
                            ErrorMessage = "Failed to start Claude Code CLI process."
                        };
                    }

                    if (!string.IsNullOrEmpty(standardInput_))
                    {
                        await process.StandardInput.WriteAsync(standardInput_);
                    }

                    process.StandardInput.Close();

                    Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                    Task<int> exitTask = Task.Run(() =>
                    {
                        process.WaitForExit();
                        return process.ExitCode;
                    });

                    Task delayTask = _timeoutSeconds > 0
                        ? Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), cancellationToken_)
                        : Task.Delay(Timeout.Infinite, cancellationToken_);

                    Task finishedTask = await Task.WhenAny(exitTask, delayTask);

                    if (finishedTask != exitTask)
                    {
                        TryKill(process);

                        string errorMessage = cancellationToken_.IsCancellationRequested
                            ? "Claude Code CLI request was cancelled."
                            : "Claude Code CLI request timed out.";

                        return new ClaudeCodeCliResponseResult
                        {
                            IsSuccess = false,
                            ExitCode = -1,
                            ErrorMessage = errorMessage
                        };
                    }

                    int exitCode = await exitTask;
                    string standardOutput = await stdoutTask;
                    string standardError = await stderrTask;

                    ClaudeCodeCliResponseResult result = new ClaudeCodeCliResponseResult
                    {
                        IsSuccess = exitCode == 0,
                        ExitCode = exitCode,
                        StandardOutput = standardOutput,
                        StandardError = standardError
                    };

                    if (!result.IsSuccess && string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        result.ErrorMessage = string.IsNullOrEmpty(standardOutput)
                            ? $"Claude Code CLI exited with code {exitCode}."
                            : standardOutput;
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = ex.Message
                };
            }
        }

        internal async Task<ClaudeCodeCliResponseResult> ExecutePersistentAsync(
            string executablePath_,
            string arguments_,
            string standardInput_,
            Dictionary<string, string> environmentVariables_ = null,
            string workingDirectory_ = null,
            CancellationToken cancellationToken_ = default)
        {
            if (string.IsNullOrEmpty(executablePath_))
            {
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = "Claude Code CLI executable path is empty."
                };
            }

            string resolvedPath = ResolveExecutablePath(executablePath_);
            if (string.IsNullOrEmpty(resolvedPath))
            {
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = $"Claude Code CLI executable '{executablePath_}' not found in PATH or specified location."
                };
            }

            await _requestLock.WaitAsync(cancellationToken_);
            try
            {
                int stderrBefore = GetStderrCount();

                bool isNewProcess = _persistentProcess == null || _persistentProcess.HasExited;

                if (!EnsurePersistentProcess(resolvedPath, arguments_, environmentVariables_, workingDirectory_))
                {
                    return new ClaudeCodeCliResponseResult
                    {
                        IsSuccess = false,
                        ExitCode = -1,
                        ErrorMessage = "Failed to start persistent Claude Code CLI process."
                    };
                }

                if (isNewProcess && !_processReady)
                {
                    await WritePersistentInputAsync(standardInput_);

                    string initLine = await WaitForInitLineAsync(cancellationToken_);
                    if (initLine == null)
                    {
                        string stderrContent = GetStderrSince(stderrBefore);
                        ResetPersistentProcess();
                        return new ClaudeCodeCliResponseResult
                        {
                            IsSuccess = false,
                            ExitCode = -1,
                            ErrorMessage = "Claude Code CLI failed to initialize." +
                                (string.IsNullOrEmpty(stderrContent) ? "" : $" Stderr: {stderrContent}"),
                            StandardError = stderrContent
                        };
                    }
                    _initLine = initLine;
                    _processReady = true;
                }
                else
                {
                    await WritePersistentInputAsync(standardInput_);
                }

                ClaudeCodeCliResponseResult result = await ReadPersistentOutputAsync(cancellationToken_);

                if (isNewProcess && !string.IsNullOrEmpty(_initLine))
                {
                    result.StandardOutput = _initLine + "\n" + (result.StandardOutput ?? string.Empty);
                }

                result.StandardError = GetStderrSince(stderrBefore);
                return result;
            }
            catch (OperationCanceledException)
            {
                ResetPersistentProcess();
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = "Claude Code CLI request was cancelled."
                };
            }
            catch (Exception ex)
            {
                ResetPersistentProcess();
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                _requestLock.Release();
            }
        }

        internal void ResetPersistentProcessPublic()
        {
            ResetPersistentProcess();
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
                return;

            ResetPersistentProcess();
            _requestLock.Dispose();
            _disposed = true;
        }

        private void TryKill(Process process_)
        {
            try
            {
                if (process_ != null && !process_.HasExited)
                    process_.Kill();
            }
            catch { }
        }

        private bool EnsurePersistentProcess(
            string executablePath_,
            string arguments_,
            Dictionary<string, string> environmentVariables_,
            string workingDirectory_)
        {
            string signature = BuildPersistentSignature(executablePath_, arguments_, workingDirectory_, environmentVariables_);
            if (_persistentProcess != null && !_persistentProcess.HasExited && string.Equals(_persistentSignature, signature, StringComparison.Ordinal))
                return true;

            ResetPersistentProcess();

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath_,
                Arguments = arguments_ ?? string.Empty,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (!string.IsNullOrEmpty(workingDirectory_))
                startInfo.WorkingDirectory = workingDirectory_;

            EnsurePathEnvironmentVariable(startInfo);

            if (environmentVariables_ != null)
            {
                foreach (KeyValuePair<string, string> kvp in environmentVariables_)
                {
                    if (!string.IsNullOrEmpty(kvp.Key))
                        startInfo.EnvironmentVariables[kvp.Key] = kvp.Value ?? string.Empty;
                }
            }

            Process process = new Process();
            process.StartInfo = startInfo;

            if (!process.Start())
            {
                process.Dispose();
                return false;
            }

            _persistentProcess = process;
            _persistentInput = process.StandardInput;
            _persistentOutput = process.StandardOutput;
            _persistentSignature = signature;
            ResetStderrBuffer();
            StartStderrReader(process);
            return true;
        }

        private async Task WritePersistentInputAsync(string standardInput_)
        {
            if (_persistentInput == null)
                return;

            if (!string.IsNullOrEmpty(standardInput_))
            {
                await _persistentInput.WriteAsync(standardInput_);
            }

            await _persistentInput.WriteLineAsync();
            await _persistentInput.FlushAsync();
        }

        private async Task<ClaudeCodeCliResponseResult> ReadPersistentOutputAsync(CancellationToken cancellationToken_)
        {
            if (_persistentProcess == null || _persistentOutput == null)
            {
                return new ClaudeCodeCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = "Persistent Claude Code CLI process is not available."
                };
            }

            DateTime startedAt = DateTime.UtcNow;
            StringBuilder outputBuilder = new StringBuilder();

            while (true)
            {
                if (_persistentProcess.HasExited)
                {
                    ResetPersistentProcess();
                    return new ClaudeCodeCliResponseResult
                    {
                        IsSuccess = false,
                        ExitCode = -1,
                        ErrorMessage = "Claude Code CLI process exited unexpectedly."
                    };
                }

                Task<string> readTask = _persistentOutput.ReadLineAsync();
                Task delayTask = BuildTimeoutTask(startedAt, cancellationToken_);
                Task finishedTask = await Task.WhenAny(readTask, delayTask);

                if (finishedTask != readTask)
                {
                    ResetPersistentProcess();
                    string errorMessage = cancellationToken_.IsCancellationRequested
                        ? "Claude Code CLI request was cancelled."
                        : "Claude Code CLI request timed out.";
                    return new ClaudeCodeCliResponseResult
                    {
                        IsSuccess = false,
                        ExitCode = -1,
                        ErrorMessage = errorMessage
                    };
                }

                string line = await readTask;
                if (line == null)
                {
                    ResetPersistentProcess();
                    return new ClaudeCodeCliResponseResult
                    {
                        IsSuccess = false,
                        ExitCode = -1,
                        ErrorMessage = "Claude Code CLI process ended without response."
                    };
                }

                if (outputBuilder.Length > 0)
                    outputBuilder.AppendLine();
                outputBuilder.Append(line);

                if (IsTurnCompletedLine(line))
                    break;
            }

            return new ClaudeCodeCliResponseResult
            {
                IsSuccess = true,
                ExitCode = 0,
                StandardOutput = outputBuilder.ToString()
            };
        }

        private Task BuildTimeoutTask(DateTime startedAt_, CancellationToken cancellationToken_)
        {
            if (_timeoutSeconds <= 0)
                return Task.Delay(Timeout.Infinite, cancellationToken_);

            TimeSpan elapsed = DateTime.UtcNow - startedAt_;
            TimeSpan remaining = TimeSpan.FromSeconds(_timeoutSeconds) - elapsed;
            if (remaining <= TimeSpan.Zero)
                return Task.Delay(0, cancellationToken_);

            return Task.Delay(remaining, cancellationToken_);
        }

        private async Task<string> WaitForInitLineAsync(CancellationToken cancellationToken_)
        {
            if (_persistentProcess == null || _persistentOutput == null)
                return null;

            DateTime startedAt = DateTime.UtcNow;

            while (true)
            {
                if (_persistentProcess.HasExited)
                    return null;

                Task<string> readTask = _persistentOutput.ReadLineAsync();
                Task delayTask = BuildTimeoutTask(startedAt, cancellationToken_);
                Task finishedTask = await Task.WhenAny(readTask, delayTask);

                if (finishedTask != readTask)
                    return null;

                string line = await readTask;
                if (line == null)
                    return null;

                if (IsInitLine(line))
                    return line;
            }
        }

        private bool IsInitLine(string line_)
        {
            if (string.IsNullOrEmpty(line_))
                return false;

            return line_.IndexOf("\"type\":\"system\"", StringComparison.Ordinal) >= 0 ||
                   line_.IndexOf("\"type\": \"system\"", StringComparison.Ordinal) >= 0;
        }

        private bool IsTurnCompletedLine(string line_)
        {
            if (string.IsNullOrEmpty(line_))
                return false;

            return line_.IndexOf("\"type\":\"result\"", StringComparison.Ordinal) >= 0 ||
                   line_.IndexOf("\"type\": \"result\"", StringComparison.Ordinal) >= 0;
        }

        private string BuildPersistentSignature(
            string executablePath_,
            string arguments_,
            string workingDirectory_,
            Dictionary<string, string> environmentVariables_)
        {
            StringBuilder signatureBuilder = new StringBuilder();
            signatureBuilder.Append(executablePath_ ?? string.Empty);
            signatureBuilder.Append("|").Append(arguments_ ?? string.Empty);
            signatureBuilder.Append("|").Append(workingDirectory_ ?? string.Empty);

            if (environmentVariables_ != null && environmentVariables_.Count > 0)
            {
                List<string> keys = new List<string>(environmentVariables_.Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (string key in keys)
                {
                    signatureBuilder.Append("|").Append(key).Append("=").Append(environmentVariables_[key] ?? string.Empty);
                }
            }

            return signatureBuilder.ToString();
        }

        private void StartStderrReader(Process process_)
        {
            if (process_ == null)
                return;

            _stderrReaderTask = Task.Run(async () =>
            {
                while (!process_.HasExited)
                {
                    string line = await process_.StandardError.ReadLineAsync();
                    if (line == null)
                        break;

                    lock (_stderrLock)
                    {
                        _stderrLines.Add(line);
                    }
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
                if (startIndex_ < 0 || startIndex_ >= _stderrLines.Count)
                    return string.Empty;

                StringBuilder builder = new StringBuilder();
                for (int i = startIndex_; i < _stderrLines.Count; i++)
                {
                    if (builder.Length > 0)
                        builder.AppendLine();
                    builder.Append(_stderrLines[i]);
                }

                return builder.ToString();
            }
        }

        private void ResetPersistentProcess()
        {
            _processReady = false;
            _initLine = null;

            if (_persistentProcess == null)
                return;

            TryKill(_persistentProcess);
            _persistentProcess.Dispose();
            _persistentProcess = null;
            _persistentInput = null;
            _persistentOutput = null;
            _persistentSignature = null;
            ResetStderrBuffer();
        }

        private string ResolveExecutablePath(string executablePath_)
        {
            return FindExecutablePathInternal(executablePath_);
        }

        private void EnsurePathEnvironmentVariable(ProcessStartInfo startInfo_)
        {
            if (startInfo_ == null)
                return;

            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            List<string> additionalPaths = new List<string>();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            additionalPaths.Add("/opt/homebrew/bin");
            additionalPaths.Add("/usr/local/bin");
            additionalPaths.Add("/usr/bin");
            string homeDir = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(homeDir))
            {
                additionalPaths.Add($"{homeDir}/.claude/local");
                additionalPaths.Add($"{homeDir}/.local/bin");
            }
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            additionalPaths.Add("/usr/local/bin");
            additionalPaths.Add("/usr/bin");
            string homeDir = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(homeDir))
            {
                additionalPaths.Add($"{homeDir}/.claude/local");
                additionalPaths.Add($"{homeDir}/.local/bin");
            }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                additionalPaths.Add(System.IO.Path.Combine(localAppData, "Programs", "claude-code"));
                additionalPaths.Add(System.IO.Path.Combine(localAppData, "AnthropicClaude"));
            }
#endif

            string pathSeparator = System.IO.Path.PathSeparator.ToString();
            List<string> existingPathParts = new List<string>(currentPath.Split(new[] { pathSeparator }, StringSplitOptions.RemoveEmptyEntries));

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

        internal static string FindExecutablePathInternal(string executablePath_)
        {
            return CliPathUtility.FindExecutablePath(executablePath_);
        }

        internal static string FindClaudeCodeExecutablePathInternal()
        {
            string[] commonPaths = GetCommonClaudeCodePathsInternal();
            string found = CliPathUtility.FindFirstExistingPath(commonPaths);
            if (!string.IsNullOrEmpty(found))
                return found;

            string executableName = GetDefaultExecutableNameInternal();
            string pathResult = FindExecutablePathInternal(executableName);
            if (!string.IsNullOrEmpty(pathResult))
                return pathResult;

            return CliPathUtility.FindExecutableViaShell(executableName);
        }

        internal static string[] GetCommonClaudeCodePathsInternal()
        {
            List<string> candidates = new List<string>();

            if (Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.OSXPlayer)
            {
                candidates.Add("/opt/homebrew/bin/claude");
                candidates.Add("/usr/local/bin/claude");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/.claude/local/claude");
                    candidates.Add($"{homeDir}/.local/bin/claude");
                }
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor
                || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                candidates.Add("/usr/local/bin/claude");
                candidates.Add("/usr/bin/claude");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/.claude/local/claude");
                    candidates.Add($"{homeDir}/.local/bin/claude");
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(localAppData))
                {
                    candidates.Add(Path.Combine(localAppData, "Programs", "claude-code", "claude.exe"));
                    candidates.Add(Path.Combine(localAppData, "AnthropicClaude", "claude.exe"));
                }
            }

            return candidates.ToArray();
        }

        internal static string GetDefaultExecutableNameInternal()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer)
                return "claude.exe";

            return "claude";
        }

        internal static string FindNodeExecutablePathInternal()
        {
            return CliPathUtility.FindNodeExecutablePath();
        }

        internal static string[] GetCommonNodePathsInternal()
        {
            return CliPathUtility.GetCommonNodePaths();
        }
    }
}
