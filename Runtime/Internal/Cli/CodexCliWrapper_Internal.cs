using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    public partial class CodexCliWrapper : IDisposable
    {
        private readonly int _timeoutSeconds;
        private readonly SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);
        private bool _disposed;
        private bool _hasActiveSession;
        private Process _persistentProcess;
        private StreamWriter _persistentInput;
        private StreamReader _persistentOutput;
        private string _persistentSignature;
        private Task _stderrReaderTask;
        private readonly List<string> _stderrLines = new List<string>();
        private readonly object _stderrLock = new object();

        internal CodexCliWrapper(int timeoutSeconds_ = 60)
        {
            _timeoutSeconds = timeoutSeconds_;
        }

        internal async Task<CodexCliResponseResult> ExecuteAsync(
            string executablePath_,
            string arguments_,
            string standardInput_,
            Dictionary<string, string> environmentVariables_ = null,
            string workingDirectory_ = null,
            string nodeExecutablePath_ = null,
            CancellationToken cancellationToken_ = default)
        {
            if (string.IsNullOrEmpty(executablePath_))
            {
                return new CodexCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = "Codex CLI executable path is empty."
                };
            }

            string resolvedPath = ResolveExecutablePath(executablePath_);
            if (string.IsNullOrEmpty(resolvedPath))
            {
                return new CodexCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = $"Codex CLI executable '{executablePath_}' not found in PATH or specified location."
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

                EnsurePathEnvironmentVariable(startInfo, nodeExecutablePath_);

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
                        return new CodexCliResponseResult
                        {
                            IsSuccess = false,
                            ExitCode = -1,
                            ErrorMessage = "Failed to start Codex CLI process."
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
                            ? "Codex CLI request was cancelled."
                            : "Codex CLI request timed out.";

                        return new CodexCliResponseResult
                        {
                            IsSuccess = false,
                            ExitCode = -1,
                            ErrorMessage = errorMessage
                        };
                    }

                    int exitCode = await exitTask;
                    string standardOutput = await stdoutTask;
                    string standardError = await stderrTask;

                    CodexCliResponseResult result = new CodexCliResponseResult
                    {
                        IsSuccess = exitCode == 0,
                        ExitCode = exitCode,
                        StandardOutput = standardOutput,
                        StandardError = standardError
                    };

                    if (!result.IsSuccess && string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        result.ErrorMessage = string.IsNullOrEmpty(standardError)
                            ? $"Codex CLI exited with code {exitCode}."
                            : standardError;
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                return new CodexCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = ex.Message
                };
            }
        }

        internal async Task<CodexCliResponseResult> ExecutePersistentAsync(
            string executablePath_,
            string arguments_,
            string standardInput_,
            Dictionary<string, string> environmentVariables_ = null,
            string workingDirectory_ = null,
            string nodeExecutablePath_ = null,
            CancellationToken cancellationToken_ = default)
        {
            string effectiveArgs = _hasActiveSession
                ? TransformToResumeArguments(arguments_)
                : arguments_;

            CodexCliResponseResult result = await ExecuteAsync(
                executablePath_,
                effectiveArgs,
                standardInput_,
                environmentVariables_,
                workingDirectory_,
                nodeExecutablePath_,
                cancellationToken_);

            if (result.IsSuccess)
                _hasActiveSession = true;

            return result;
        }

        internal void ResetPersistentProcessPublic()
        {
            ResetPersistentProcess();
        }

        private string TransformToResumeArguments(string arguments_)
        {
            string model = ExtractModelFromArguments(arguments_);
            string modelArg = string.IsNullOrEmpty(model)
                ? string.Empty
                : $" -c model={model}";

            return $"exec --json resume --last{modelArg} -";
        }

        private string ExtractModelFromArguments(string arguments_)
        {
            if (string.IsNullOrEmpty(arguments_))
                return null;

            string[] tokens = arguments_.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                if (string.Equals(tokens[i], "-m", StringComparison.Ordinal) ||
                    string.Equals(tokens[i], "--model", StringComparison.Ordinal))
                    return tokens[i + 1];
            }

            return null;
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
            catch
            {
            }
        }

        private bool EnsurePersistentProcess(
            string executablePath_,
            string arguments_,
            Dictionary<string, string> environmentVariables_,
            string workingDirectory_,
            string nodeExecutablePath_)
        {
            string signature = BuildPersistentSignature(executablePath_, arguments_, workingDirectory_, nodeExecutablePath_, environmentVariables_);
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

            EnsurePathEnvironmentVariable(startInfo, nodeExecutablePath_);

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

        private async Task<CodexCliResponseResult> ReadPersistentOutputAsync(CancellationToken cancellationToken_)
        {
            if (_persistentProcess == null || _persistentOutput == null)
            {
                return new CodexCliResponseResult
                {
                    IsSuccess = false,
                    ExitCode = -1,
                    ErrorMessage = "Persistent Codex CLI process is not available."
                };
            }

            DateTime startedAt = DateTime.UtcNow;
            StringBuilder outputBuilder = new StringBuilder();

            while (true)
            {
                if (_persistentProcess.HasExited)
                {
                    ResetPersistentProcess();
                    return new CodexCliResponseResult
                    {
                        IsSuccess = false,
                        ExitCode = -1,
                        ErrorMessage = "Codex CLI process exited unexpectedly."
                    };
                }

                Task<string> readTask = _persistentOutput.ReadLineAsync();
                Task delayTask = BuildTimeoutTask(startedAt, cancellationToken_);
                Task finishedTask = await Task.WhenAny(readTask, delayTask);

                if (finishedTask != readTask)
                {
                    ResetPersistentProcess();
                    string errorMessage = cancellationToken_.IsCancellationRequested
                        ? "Codex CLI request was cancelled."
                        : "Codex CLI request timed out.";
                    return new CodexCliResponseResult
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
                    return new CodexCliResponseResult
                    {
                        IsSuccess = false,
                        ExitCode = -1,
                        ErrorMessage = "Codex CLI process ended without response."
                    };
                }

                if (outputBuilder.Length > 0)
                    outputBuilder.AppendLine();
                outputBuilder.Append(line);

                if (IsTurnCompletedLine(line))
                    break;
            }

            return new CodexCliResponseResult
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

        private bool IsTurnCompletedLine(string line_)
        {
            if (string.IsNullOrEmpty(line_))
                return false;

            return line_.IndexOf("\"type\":\"turn.completed\"", StringComparison.Ordinal) >= 0 ||
                   line_.IndexOf("\"type\": \"turn.completed\"", StringComparison.Ordinal) >= 0;
        }

        private string BuildPersistentSignature(
            string executablePath_,
            string arguments_,
            string workingDirectory_,
            string nodeExecutablePath_,
            Dictionary<string, string> environmentVariables_)
        {
            StringBuilder signatureBuilder = new StringBuilder();
            signatureBuilder.Append(executablePath_ ?? string.Empty);
            signatureBuilder.Append("|").Append(arguments_ ?? string.Empty);
            signatureBuilder.Append("|").Append(workingDirectory_ ?? string.Empty);
            signatureBuilder.Append("|").Append(nodeExecutablePath_ ?? string.Empty);

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
            _hasActiveSession = false;

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

        private void EnsurePathEnvironmentVariable(ProcessStartInfo startInfo_, string nodeExecutablePath_)
        {
            if (startInfo_ == null)
                return;

            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            List<string> additionalPaths = new List<string>();

            if (!string.IsNullOrEmpty(nodeExecutablePath_))
            {
                string nodeDir = System.IO.Path.GetDirectoryName(nodeExecutablePath_);
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

        internal static string FindCodexExecutablePathInternal()
        {
            string[] commonPaths = GetCommonCodexPathsInternal();
            string found = CliPathUtility.FindFirstExistingPath(commonPaths);
            if (!string.IsNullOrEmpty(found))
                return found;

            string executableName = GetDefaultExecutableNameInternal();
            string pathResult = FindExecutablePathInternal(executableName);
            if (!string.IsNullOrEmpty(pathResult))
                return pathResult;

            return CliPathUtility.FindExecutableViaShell(executableName);
        }

        internal static string FindNodeExecutablePathInternal()
        {
            return CliPathUtility.FindNodeExecutablePath();
        }

        internal static string[] GetCommonNodePathsInternal()
        {
            return CliPathUtility.GetCommonNodePaths();
        }

        internal static string[] GetCommonCodexPathsInternal()
        {
            List<string> candidates = new List<string>();

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                candidates.Add("/opt/homebrew/bin/codex");
                candidates.Add("/usr/local/bin/codex");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/.local/bin/codex");
                }
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                candidates.Add("/usr/local/bin/codex");
                candidates.Add("/usr/bin/codex");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/.local/bin/codex");
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                candidates.Add(Path.Combine(programFiles, "OpenAI", "codex.exe"));
                candidates.Add(Path.Combine(localAppData, "Programs", "OpenAI", "codex.exe"));
                candidates.Add(Path.Combine(localAppData, "Programs", "codex", "codex.exe"));
            }

            return candidates.ToArray();
        }

        internal static string FindFirstExistingPathInternal(string[] paths_)
        {
            return CliPathUtility.FindFirstExistingPath(paths_);
        }

        internal static string GetDefaultExecutableNameInternal()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return "codex.exe";

            return "codex";
        }
    }
}
