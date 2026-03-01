using UnityEngine;

namespace Weppy.AIProvider
{
    internal static class ImageProviderFactory
    {
        internal static ImageProviderAbstract Create(ImageProviderType providerType_, ImageProviderSettings settings_)
        {
            if (settings_ == null)
            {
                Debug.LogError($"Image generation provider:{providerType_} settings cannot be null.");
                return null;
            }

            switch (providerType_)
            {
                case ImageProviderType.OPEN_AI:
                {
                    return new ImageProviderOpenAI(settings_);
                }
                case ImageProviderType.GOOGLE_GEMINI:
                {
                    return new ImageProviderGoogleGemini(settings_);
                }
                case ImageProviderType.GOOGLE_IMAGEN:
                {
                    return new ImageProviderGoogleImagen(settings_);
                }
                case ImageProviderType.OPEN_ROUTER:
                {
                    return new ImageProviderOpenRouter(settings_);
                }
            }

            Debug.LogError($"Image generation provider:{providerType_} is not supported.");
            return null;
        }
    }
}
