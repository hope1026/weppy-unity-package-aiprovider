using UnityEngine;

namespace Weppy.AIProvider
{
    internal static class BgRemovalProviderFactory
    {
        internal static BgRemovalProviderAbstract Create(BgRemovalProviderType providerType_, BgRemovalProviderSettings settings_)
        {
            if (settings_ == null)
            {
                Debug.LogError($"Background removal provider:{providerType_} settings cannot be null.");
                return null;
            }

            switch (providerType_)
            {
                case BgRemovalProviderType.REMOVE_BG:
                {
                    return new BgRemovalProviderRemoveBg(settings_);
                }
            }

            Debug.LogError($"Background removal provider:{providerType_} is not supported.");
            return null;
        }
    }
}