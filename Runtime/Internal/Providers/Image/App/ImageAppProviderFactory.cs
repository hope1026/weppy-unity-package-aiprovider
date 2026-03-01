using UnityEngine;

namespace Weppy.AIProvider
{
    internal static class ImageAppProviderFactory
    {
        internal static ImageAppProviderAbstract Create(ImageAppProviderType providerType_, ImageAppProviderSettings settings_)
        {
            if (settings_ == null)
            {
                Debug.LogError($"Image app provider:{providerType_} settings cannot be null.");
                return null;
            }

            switch (providerType_)
            {
                case ImageAppProviderType.CODEX_APP:
                {
                    return new ImageAppProviderCodex(settings_);
                }
            }

            Debug.LogError($"Image app provider:{providerType_} is not supported.");
            return null;
        }
    }
}
