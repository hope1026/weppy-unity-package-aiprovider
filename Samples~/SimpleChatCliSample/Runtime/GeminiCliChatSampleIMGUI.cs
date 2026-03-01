using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Weppy.AIProvider;

namespace Weppy.AIProvider.Samples
{
    public class GeminiCliChatSampleIMGUI : MonoBehaviour
    {
        [SerializeField] private ChatCliProviderType _providerType = ChatCliProviderType.GEMINI_CLI;
        [SerializeField] private string _model = GeminiCliWrapper.AUTO_MODEL_ID;
        [SerializeField] private int _timeoutSeconds = 120;

        private string _promptText = "";
        private string _responseText = "";
        private Vector2 _scrollPosition;
        private bool _isLoading;
        private string _resolvedCliPath = "";
        private string _resolvedNodePath = "";
        private string _cliPathInput = "";
        private string _nodePathInput = "";

        private ChatCliProviderManager _providerManager;
        private CancellationTokenSource _cancellationTokenSource;

        private static readonly string[] ProviderLabels = new string[]
        {
            "Codex CLI",
            "Claude Code CLI",
            "Gemini CLI"
        };

        private void OnEnable()
        {
            InitializeProvider();
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _providerManager?.Dispose();
            _providerManager = null;
        }

        private void InitializeProvider()
        {
            _resolvedCliPath = ResolveCliPath(_providerType);
            _resolvedNodePath = ResolveNodePath(_providerType);

            if (string.IsNullOrEmpty(_cliPathInput))
                _cliPathInput = _resolvedCliPath;

            if (string.IsNullOrEmpty(_nodePathInput))
                _nodePathInput = _resolvedNodePath;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _providerManager?.Dispose();
            _providerManager = null;

            _providerManager = new ChatCliProviderManager();

            string effectiveCliPath = GetEffectiveCliPath();
            string effectiveNodePath = GetEffectiveNodePath();

            AIProviderLogger.LogVerbose($"[Sample][CLI] Initialize provider. Type={_providerType}, Platform={Application.platform}");
            AIProviderLogger.LogVerbose($"[Sample][CLI] Resolved paths. Cli='{_resolvedCliPath}', Node='{_resolvedNodePath}'");
            AIProviderLogger.LogVerbose($"[Sample][CLI] Effective paths. Cli='{effectiveCliPath}', Node='{effectiveNodePath}'");

            if (string.IsNullOrEmpty(effectiveCliPath))
                AIProviderLogger.LogWarning("[Sample][CLI] CLI executable path is empty.");

            ChatCliProviderSettings settings = new ChatCliProviderSettings()
            {
                DefaultModel = _model,
                TimeoutSeconds = _timeoutSeconds,
                UseApiKey = false,
                CliExecutablePath = effectiveCliPath,
                NodeExecutablePath = effectiveNodePath
            };

            _providerManager.AddProvider(_providerType, settings);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));

            GUILayout.Label("Simple Chat CLI Sample", GUI.skin.box);
            GUILayout.Space(10);

            bool hasModel = !string.IsNullOrEmpty(_model);
            bool hasCliPath = !string.IsNullOrEmpty(GetEffectiveCliPath());

            DrawAuthenticationNotice();

            int selectedProviderIndex = GetProviderIndex(_providerType);
            int newProviderIndex = GUILayout.SelectionGrid(selectedProviderIndex, ProviderLabels, 3);
            if (newProviderIndex != selectedProviderIndex)
            {
                _providerType = GetProviderTypeFromIndex(newProviderIndex);
                InitializeProvider();
            }

            GUILayout.Space(10);

            if (!hasCliPath || !hasModel)
            {
                GUIStyle warningStyle = new GUIStyle(GUI.skin.box);
                warningStyle.normal.textColor = Color.yellow;
                warningStyle.wordWrap = true;

                string warningMessage = "Missing configuration:\n";
                if (!hasCliPath) warningMessage += "- CLI executable not found\n";
                if (!hasModel) warningMessage += "- Model is required\n";
                warningMessage += "\nPlease set them below or in the Inspector.";

                GUILayout.Label(warningMessage, warningStyle);
                GUILayout.Space(10);
            }

            GUILayout.Label("Auto-Detected CLI Path:");
            string cliPathLabel = string.IsNullOrEmpty(_resolvedCliPath) ? "(Not Found)" : _resolvedCliPath;
            GUILayout.Label(cliPathLabel, GUI.skin.box);

            if (_providerType == ChatCliProviderType.CODEX_CLI)
            {
                GUILayout.Label("Auto-Detected Node Path:");
                string nodePathLabel = string.IsNullOrEmpty(_resolvedNodePath) ? "(Not Found)" : _resolvedNodePath;
                GUILayout.Label(nodePathLabel, GUI.skin.box);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Auto Detect Paths", GUILayout.Height(24)))
            {
                AutoDetectPaths();
            }

            GUILayout.Space(6);

            GUILayout.Label("CLI Path (Manual Override):");
            _cliPathInput = GUILayout.TextField(_cliPathInput);

            if (_providerType == ChatCliProviderType.CODEX_CLI)
            {
                GUILayout.Label("Node Path (Manual Override):");
                _nodePathInput = GUILayout.TextField(_nodePathInput);
            }

            if (GUILayout.Button("Apply Paths", GUILayout.Height(24)))
            {
                ApplyManualPaths();
            }

            GUILayout.Space(10);

            GUILayout.Label("Prompt:");
            _promptText = GUILayout.TextArea(_promptText, GUILayout.Height(60));

            GUILayout.Space(10);

            GUI.enabled = !_isLoading && hasModel && hasCliPath;
            if (GUILayout.Button(_isLoading ? "Sending..." : "Send", GUILayout.Height(30)))
            {
                SendMessage();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            GUILayout.Label("Response:");
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUI.skin.box, GUILayout.ExpandHeight(true));
            GUILayout.Label(_responseText, GUILayout.ExpandWidth(true));
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private async void SendMessage()
        {
            if (string.IsNullOrEmpty(_promptText))
            {
                _responseText = "Please enter a prompt.";
                return;
            }

            _isLoading = true;
            _responseText = "Sending...";

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            ChatCliRequestPayload requestPayload = new ChatCliRequestPayload()
                .AddUserMessage(_promptText);
            requestPayload.Model = _model;

            List<ChatCliRequestProviderTarget> targets = new List<ChatCliRequestProviderTarget>
            {
                new ChatCliRequestProviderTarget
                {
                    ProviderType = _providerType,
                    Model = _model,
                    Priority = 0
                }
            };

            ChatCliRequestParams requestParams = new ChatCliRequestParams
            {
                Providers = targets,
                RequestPayload = requestPayload
            };

            ChatCliResponse response = await _providerManager.SendMessageWithProvidersAsync(
                requestParams,
                _cancellationTokenSource.Token
            );

            if (response.IsSuccess)
            {
                _responseText = response.Content;
            }
            else
            {
                _responseText = $"Error: {response.ErrorMessage}";
            }

            _isLoading = false;
        }

        private string ResolveCliPath(ChatCliProviderType providerType_)
        {
            if (providerType_ == ChatCliProviderType.CODEX_CLI)
                return CodexCliWrapper.FindCodexExecutablePath();

            if (providerType_ == ChatCliProviderType.CLAUDE_CODE_CLI)
                return ClaudeCodeCliWrapper.FindClaudeCodeExecutablePath();

            return GeminiCliWrapper.FindGeminiExecutablePath();
        }

        private string ResolveNodePath(ChatCliProviderType providerType_)
        {
            if (providerType_ != ChatCliProviderType.CODEX_CLI)
                return string.Empty;

            return CodexCliWrapper.FindNodeExecutablePath();
        }

        private void AutoDetectPaths()
        {
            AIProviderLogger.LogVerbose($"[Sample][CLI] Auto-detect paths requested. Type={_providerType}, Platform={Application.platform}");

            _resolvedCliPath = ResolveCliPath(_providerType);
            _resolvedNodePath = ResolveNodePath(_providerType);

            if (!string.IsNullOrEmpty(_resolvedCliPath))
                _cliPathInput = _resolvedCliPath;

            if (!string.IsNullOrEmpty(_resolvedNodePath))
                _nodePathInput = _resolvedNodePath;

            AIProviderLogger.LogVerbose($"[Sample][CLI] Auto-detect result. Cli='{_resolvedCliPath}', Node='{_resolvedNodePath}'");

            if (string.IsNullOrEmpty(_resolvedCliPath))
                AIProviderLogger.LogWarning("[Sample][CLI] Auto-detect failed to find CLI executable.");

            if (_providerType == ChatCliProviderType.CODEX_CLI && string.IsNullOrEmpty(_resolvedNodePath))
                AIProviderLogger.LogWarning("[Sample][CLI] Auto-detect failed to find Node executable.");

            ApplyManualPaths();
        }

        private void ApplyManualPaths()
        {
            InitializeProvider();
        }

        private string GetEffectiveCliPath()
        {
            if (!string.IsNullOrEmpty(_cliPathInput))
                return _cliPathInput;

            return _resolvedCliPath;
        }

        private string GetEffectiveNodePath()
        {
            if (_providerType != ChatCliProviderType.CODEX_CLI)
                return string.Empty;

            if (!string.IsNullOrEmpty(_nodePathInput))
                return _nodePathInput;

            return _resolvedNodePath;
        }

        private int GetProviderIndex(ChatCliProviderType providerType_)
        {
            if (providerType_ == ChatCliProviderType.CODEX_CLI)
                return 0;

            if (providerType_ == ChatCliProviderType.CLAUDE_CODE_CLI)
                return 1;

            return 2;
        }

        private ChatCliProviderType GetProviderTypeFromIndex(int index_)
        {
            if (index_ == 0)
                return ChatCliProviderType.CODEX_CLI;

            if (index_ == 1)
                return ChatCliProviderType.CLAUDE_CODE_CLI;

            return ChatCliProviderType.GEMINI_CLI;
        }

        private void DrawAuthenticationNotice()
        {
            GUIStyle noticeStyle = new GUIStyle(GUI.skin.box);
            noticeStyle.wordWrap = true;
            GUILayout.Label("This sample requires a pre-authenticated CLI. Please log in with the selected CLI before sending prompts.", noticeStyle);
            GUILayout.Space(10);
        }
    }
}
