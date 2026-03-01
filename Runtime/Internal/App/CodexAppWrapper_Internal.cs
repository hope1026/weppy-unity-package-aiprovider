using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Weppy.AIProvider
{
    public partial class CodexAppWrapper
    {
        internal static string FindCodexAppExecutablePathInternal()
        {
            string[] commonPaths = GetCommonCodexAppPathsInternal();
            string found = CliPathUtility.FindFirstExistingPath(commonPaths);
            if (!string.IsNullOrEmpty(found))
                return found;

            string executableName = GetDefaultExecutableNameInternal();
            string pathResult = CliPathUtility.FindExecutablePath(executableName);
            if (!string.IsNullOrEmpty(pathResult))
                return pathResult;

            return CliPathUtility.FindExecutableViaShell(executableName);
        }

        internal static string[] GetCommonCodexAppPathsInternal()
        {
            List<string> candidates = new List<string>();

            if (Application.platform == RuntimePlatform.OSXEditor
                || Application.platform == RuntimePlatform.OSXPlayer)
            {
                candidates.Add("/Applications/Codex.app/Contents/Resources/codex");
                candidates.Add("/Applications/Codex.app/Contents/MacOS/Codex");
                candidates.Add("/opt/homebrew/bin/codex");
                candidates.Add("/usr/local/bin/codex");
                candidates.Add("/usr/bin/codex");

                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/Applications/Codex.app/Contents/Resources/codex");
                    candidates.Add($"{homeDir}/Applications/Codex.app/Contents/MacOS/Codex");
                    candidates.Add($"{homeDir}/.local/bin/codex");
                }
            }
            else if (Application.platform == RuntimePlatform.LinuxEditor
                     || Application.platform == RuntimePlatform.LinuxPlayer)
            {
                candidates.Add("/usr/local/bin/codex");
                candidates.Add("/usr/bin/codex");

                string homeDir = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(homeDir))
                {
                    candidates.Add($"{homeDir}/.local/bin/codex");
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor
                     || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                if (!string.IsNullOrEmpty(programFiles))
                {
                    candidates.Add(Path.Combine(programFiles, "OpenAI", "codex.exe"));
                    candidates.Add(Path.Combine(programFiles, "Codex", "codex.exe"));
                }

                if (!string.IsNullOrEmpty(localAppData))
                {
                    candidates.Add(Path.Combine(localAppData, "Programs", "OpenAI", "codex.exe"));
                    candidates.Add(Path.Combine(localAppData, "Programs", "Codex", "codex.exe"));
                }
            }

            return candidates.ToArray();
        }

        internal static string GetDefaultExecutableNameInternal()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer)
                return "codex.exe";

            return "codex";
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
