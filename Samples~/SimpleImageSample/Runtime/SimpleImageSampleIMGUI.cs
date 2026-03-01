using System.Threading;
using UnityEngine;

namespace Weppy.AIProvider
{
    public class SimpleImageSampleIMGUI : MonoBehaviour
    {
        [SerializeField] private ImageProviderType _providerType = ImageProviderType.OPEN_AI;
        [SerializeField] private string _apiKey;
        [SerializeField] private string _model = ImageModelPresets.OpenAI.GPT_IMAGE_1;

        private string _promptText = "";
        private string _statusText = "";
        private Vector2 _scrollPosition;
        private bool _isLoading;
        private Texture2D _generatedTexture;

        private ImageProviderManager _providerManager;
        private CancellationTokenSource _cancellationTokenSource;

        private static readonly string[] ProviderLabels = new string[]
        {
            "OpenAI",
            "Google Gemini",
            "Google Imagen",
            "OpenRouter"
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
            CleanupTexture();
        }

        private void InitializeProvider()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _providerManager?.Dispose();
            _providerManager = null;

            _providerManager = new ImageProviderManager();

            ImageProviderSettings settings = new ImageProviderSettings(_apiKey)
            {
                DefaultModel = _model
            };

            _providerManager.AddProvider(_providerType, settings);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));

            GUILayout.Label("Simple Image Generation Sample", GUI.skin.box);
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

            int selectedProviderIndex = GetProviderIndex(_providerType);
            int newProviderIndex = GUILayout.SelectionGrid(selectedProviderIndex, ProviderLabels, 4);
            if (newProviderIndex != selectedProviderIndex)
            {
                _providerType = GetProviderTypeFromIndex(newProviderIndex);
                _model = GetDefaultModelForProvider(_providerType);
                InitializeProvider();
            }

            GUILayout.Space(10);

            GUILayout.Label($"Model: {_model}");
            GUILayout.Space(10);

            GUILayout.Label("Prompt:");
            _promptText = GUILayout.TextArea(_promptText, GUILayout.Height(60));

            GUILayout.Space(10);

            GUI.enabled = !_isLoading && hasApiKey && hasModel;
            if (GUILayout.Button(_isLoading ? "Generating..." : "Generate Image", GUILayout.Height(30)))
            {
                GenerateImage();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            if (!string.IsNullOrEmpty(_statusText))
            {
                GUILayout.Label(_statusText, GUI.skin.box);
                GUILayout.Space(10);
            }

            if (_generatedTexture != null)
            {
                GUILayout.Label("Generated Image:");
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUI.skin.box, GUILayout.ExpandHeight(true));

                float maxWidth = Screen.width - 80;
                float maxHeight = Screen.height - 400;
                float scale = Mathf.Min(maxWidth / _generatedTexture.width, maxHeight / _generatedTexture.height, 1f);
                float displayWidth = _generatedTexture.width * scale;
                float displayHeight = _generatedTexture.height * scale;

                GUILayout.Label(_generatedTexture, GUILayout.Width(displayWidth), GUILayout.Height(displayHeight));

                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No image generated yet.", GUI.skin.box);
            }

            GUILayout.EndArea();
        }

        private async void GenerateImage()
        {
            if (string.IsNullOrEmpty(_promptText))
            {
                _statusText = "Please enter a prompt.";
                return;
            }

            _isLoading = true;
            _statusText = "Generating...";
            CleanupTexture();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            ImageRequestPayload requestPayload = new Weppy.AIProvider.ImageRequestPayload(_promptText, _model);

            ImageResponse response = await _providerManager.GenerateImageAsync(
                requestPayload,
                _cancellationTokenSource.Token
            );

            if (response.IsSuccess && response.FirstImage != null)
            {
                _generatedTexture = response.FirstImage.CreateTexture2D();
                if (_generatedTexture != null)
                {
                    _statusText = $"Image generated successfully. ({_generatedTexture.width}x{_generatedTexture.height})";
                }
                else
                {
                    _statusText = "Failed to create texture from response.";
                }
            }
            else
            {
                _statusText = $"Error: {response.ErrorMessage}";
            }

            _isLoading = false;
        }

        private void CleanupTexture()
        {
            if (_generatedTexture != null)
            {
                Destroy(_generatedTexture);
                _generatedTexture = null;
            }
        }

        private int GetProviderIndex(ImageProviderType providerType_)
        {
            switch (providerType_)
            {
                case ImageProviderType.OPEN_AI:
                    return 0;
                case ImageProviderType.GOOGLE_GEMINI:
                    return 1;
                case ImageProviderType.GOOGLE_IMAGEN:
                    return 2;
                case ImageProviderType.OPEN_ROUTER:
                    return 3;
                default:
                    return 0;
            }
        }

        private ImageProviderType GetProviderTypeFromIndex(int index_)
        {
            switch (index_)
            {
                case 0:
                    return ImageProviderType.OPEN_AI;
                case 1:
                    return ImageProviderType.GOOGLE_GEMINI;
                case 2:
                    return ImageProviderType.GOOGLE_IMAGEN;
                case 3:
                    return ImageProviderType.OPEN_ROUTER;
                default:
                    return ImageProviderType.OPEN_AI;
            }
        }

        private string GetDefaultModelForProvider(ImageProviderType providerType_)
        {
            switch (providerType_)
            {
                case ImageProviderType.OPEN_AI:
                    return ImageModelPresets.OpenAI.GPT_IMAGE_1;
                case ImageProviderType.GOOGLE_GEMINI:
                    return ImageModelPresets.GoogleGemini.GEMINI_25_FLASH_IMAGE;
                case ImageProviderType.GOOGLE_IMAGEN:
                    return ImageModelPresets.GoogleImagen.IMAGEN_4;
                case ImageProviderType.OPEN_ROUTER:
                    return ImageModelPresets.OpenRouter.GEMINI_25_FLASH_IMAGE;
                default:
                    return ImageModelPresets.OpenAI.GPT_IMAGE_1;
            }
        }
    }
}
