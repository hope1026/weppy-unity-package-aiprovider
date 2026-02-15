using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Weppy.AIProvider.Chat
{
    internal static class CliPathUtility
    {
        internal static string FindExecutablePath(string executablePath_)
        {
            if (string.IsNullOrEmpty(executablePath_))
                return null;

            if (File.Exists(executablePath_))
                return executablePath_;

            if (Path.IsPathRooted(executablePath_))
                return File.Exists(executablePath_) ? executablePath_ : null;

            string[] extensions = GetExecutableExtensions();
            foreach (string extension in extensions)
            {
                string pathWithExtension = executablePath_ + extension;
                if (File.Exists(pathWithExtension))
                    return pathWithExtension;
            }

            string pathFromEnvironment = FindInPath(executablePath_, extensions);
            if (!string.IsNullOrEmpty(pathFromEnvironment))
                return pathFromEnvironment;

            return null;
        }

        internal static string FindFirstExistingPath(string[] paths_)
        {
            if (paths_ == null)
                return null;

            foreach (string path in paths_)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }

            return null;
        }

        internal static string[] GetExecutableExtensions()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return new[] { ".exe", ".cmd", ".bat", "" };
#else
            return new[] { "", ".sh" };
#endif
        }

        internal static string FindInPath(string executableName_, string[] extensions_)
        {
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
                return null;

            string[] paths = pathEnv.Split(Path.PathSeparator);
            string[] extensions = extensions_ ?? Array.Empty<string>();

            foreach (string basePath in paths)
            {
                if (string.IsNullOrEmpty(basePath))
                    continue;

                foreach (string extension in extensions)
                {
                    string fullPath = Path.Combine(basePath, executableName_ + extension);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return null;
        }

        internal static string FindExecutableViaShell(string executableName_)
        {
            if (string.IsNullOrEmpty(executableName_))
                return null;

            try
            {
                string fileName;
                string arguments;

                if (Application.platform == RuntimePlatform.WindowsEditor
                    || Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    fileName = "cmd.exe";
                    arguments = $"/c where {executableName_}";
                }
                else
                {
                    fileName = "/bin/sh";
                    arguments = $"-l -c \"which {executableName_}\"";
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;
                    if (!process.Start())
                        return null;

                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(5000);

                    if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                        return null;

                    string firstLine = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    if (File.Exists(firstLine))
                        return firstLine;
                }
            }
            catch
            {
            }

            return null;
        }

        internal static string FindNodeExecutablePath()
        {
            string[] commonPaths = GetCommonNodePaths();
            string found = CliPathUtility.FindFirstExistingPath(commonPaths);
            if (!string.IsNullOrEmpty(found))
                return found;

            return FindExecutablePath("node");
        }

        internal static string[] GetCommonNodePaths()
        {
            List<string> candidates = new List<string>();

            if (Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.OSXPlayer)
            {
                candidates.Add("/opt/homebrew/bin/node");
                candidates.Add("/usr/local/bin/node");
                candidates.Add("/usr/bin/node");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/.nvm/versions/node/latest/bin/node");
                    candidates.Add($"{homeDir}/.bun/bin/node");
                }
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor
                || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                candidates.Add("/usr/local/bin/node");
                candidates.Add("/usr/bin/node");
                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/.nvm/versions/node/latest/bin/node");
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (!string.IsNullOrEmpty(programFiles))
                {
                    candidates.Add(Path.Combine(programFiles, "nodejs", "node.exe"));
                }
                if (!string.IsNullOrEmpty(programFilesX86))
                {
                    candidates.Add(Path.Combine(programFilesX86, "nodejs", "node.exe"));
                }
            }

            return candidates.ToArray();
        }
    }
}
