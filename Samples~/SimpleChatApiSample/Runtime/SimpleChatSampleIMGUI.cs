using System.Text;
using System.Threading;
using UnityEngine;

namespace Weppy.AIProvider.Chat.Samples
{
    public class SimpleChatSampleIMGUI : MonoBehaviour
    {
        [SerializeField] private ChatProviderType _providerType = ChatProviderType.OPEN_AI;
        [SerializeField] private string _apiKey;
        [SerializeField] private string _model = "gpt-4o";

        private string _promptText = "";
        private string _responseText = "";
        private Vector2 _scrollPosition;
        private bool _isLoading;
        private bool _useStreaming = false;

        private ChatProviderManager _providerManager;
        private CancellationTokenSource _cancellationTokenSource;

        private void OnEnable()
        {
            InitializeProvider();
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _providerManager?.Dispose();
        }

        private void InitializeProvider()
        {
            _providerManager = new ChatProviderManager();

            ChatProviderSettings settings = new ChatProviderSettings(_apiKey)
            {
                DefaultModel = _model
            };

            _providerManager.AddProvider(_providerType, settings);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));

            GUILayout.Label("Simple Chat Sample", GUI.skin.box);
            GUILayout.Space(10);

            bool hasApiKey = !string.IsNullOrEmpty(_apiKey);
            bool hasModel = !string.IsNullOrEmpty(_model);

            if (!hasApiKey || !hasModel)
            {
                GUIStyle warningStyle = new GUIStyle(GUI.skin.box);
                warningStyle.normal.textColor = Color.yellow;
                warningStyle.wordWrap = true;

                string warningMessage = "Missing configuration:\n";
                if (!hasApiKey) warningMessage += "- API Key is required\n";
                if (!hasModel) warningMessage += "- Model is required\n";
                warningMessage += "\nPlease set them in the Inspector.";

                GUILayout.Label(warningMessage, warningStyle);
                GUILayout.Space(10);
            }

            GUILayout.Label("Prompt:");
            _promptText = GUILayout.TextArea(_promptText, GUILayout.Height(60));

            GUILayout.Space(10);

            GUI.enabled = !_isLoading;
            _useStreaming = GUILayout.Toggle(_useStreaming, " Use Streaming");
            GUI.enabled = true;

            GUILayout.Space(10);

            GUI.enabled = !_isLoading && hasApiKey && hasModel;
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
            _responseText = _useStreaming ? "Streaming..." : "Sending...";

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            ChatRequest request = new ChatRequest()
                .AddUserMessage(_promptText);

            if (_useStreaming)
            {
                await SendMessageWithStreaming(request);
            }
            else
            {
                await SendMessageNormal(request);
            }

            _isLoading = false;
        }

        private async System.Threading.Tasks.Task SendMessageNormal(ChatRequest request_)
        {
            ChatResponse response = await _providerManager.SendMessageWithProvidersAsync(
                _providerType,
                request_,
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
        }

        private async System.Threading.Tasks.Task SendMessageWithStreaming(ChatRequest request_)
        {
            StringBuilder contentBuilder = new StringBuilder();
            _responseText = "";

            try
            {
                await _providerManager.StreamMessageFromProviderAsync(
                    _providerType,
                    request_,
                    async (string chunk_) =>
                    {
                        contentBuilder.Append(chunk_);
                        _responseText = contentBuilder.ToString();
                        await System.Threading.Tasks.Task.Yield();
                    },
                    _cancellationTokenSource.Token
                );
            }
            catch (System.OperationCanceledException)
            {
                _responseText = contentBuilder.Length > 0
                    ? contentBuilder.ToString() + "\n\n[Cancelled]"
                    : "Request was cancelled.";
            }
            catch (System.Exception ex)
            {
                _responseText = contentBuilder.Length > 0
                    ? contentBuilder.ToString() + $"\n\n[Error: {ex.Message}]"
                    : $"Error: {ex.Message}";
            }
        }
    }
}
