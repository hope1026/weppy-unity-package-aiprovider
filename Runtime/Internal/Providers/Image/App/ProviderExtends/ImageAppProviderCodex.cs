using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal class ImageAppProviderCodex : ImageAppProviderAbstract
    {
        private readonly CodexAppServerClient _appClient;

        internal override ImageAppProviderType ProviderType => ImageAppProviderType.CODEX_APP;

        internal ImageAppProviderCodex(ImageAppProviderSettings settings_) : base(settings_)
        {
            _appClient = new CodexAppServerClient(_settings.TimeoutSeconds);
        }

        internal override async Task<ImageResponse> GenerateImageAsync(
            ImageRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default)
        {
            if (requestPayload_ == null)
                return ImageResponse.FromError("Request cannot be null.");

            if (string.IsNullOrWhiteSpace(requestPayload_.Prompt))
                return ImageResponse.FromError("Prompt cannot be empty.");

            if (!IsPlatformSupported())
                return ImageResponse.FromError("Codex App provider is supported on Windows/macOS editors and desktop standalone platforms.");

            try
            {
                CodexAppImageResult appResult = await _appClient.GenerateImageAsync(
                    requestPayload_,
                    model_ ?? _settings.DefaultModel,
                    _settings,
                    cancellationToken_);

                if (!appResult.IsSuccess)
                    return ImageResponse.FromError(appResult.ErrorMessage ?? "Codex App image generation failed.");

                ImageResponse response = new ImageResponse
                {
                    Model = model_ ?? _settings.DefaultModel,
                    Content = appResult.AgentText,
                    ProviderType = ImageProviderType.NONE
                };

                int index = 0;

                foreach (string base64 in appResult.Base64Images)
                {
                    response.Images.Add(new ImageResponseGeneratedImage
                    {
                        Index = index++,
                        Base64Data = base64
                    });
                }

                foreach (string path in appResult.ImagePaths)
                {
                    byte[] bytes = null;
                    try
                    {
                        bytes = System.IO.File.ReadAllBytes(path);
                    }
                    catch
                    {
                    }

                    response.Images.Add(new ImageResponseGeneratedImage
                    {
                        Index = index++,
                        Base64Data = bytes != null ? Convert.ToBase64String(bytes) : null,
                        Url = path
                    });
                }

                foreach (string url in appResult.ImageUrls)
                {
                    response.Images.Add(new ImageResponseGeneratedImage
                    {
                        Index = index++,
                        Url = url
                    });
                }

                if (response.Images.Count == 0)
                    return ImageResponse.FromError("Codex App did not return any image artifacts.");

                return response;
            }
            catch (OperationCanceledException)
            {
                return ImageResponse.FromError("Request was cancelled.");
            }
            catch (Exception ex)
            {
                return ImageResponse.FromError(ex.Message);
            }
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
            ((IDisposable)_appClient)?.Dispose();
        }
    }
}
