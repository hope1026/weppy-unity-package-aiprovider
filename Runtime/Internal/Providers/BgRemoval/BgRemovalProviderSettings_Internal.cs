using System.Collections.Generic;
using UnityEngine;

namespace Weppy.AIProvider
{
    public partial class BgRemovalProviderSettings
    {
        public BgRemovalProviderSettings() { }

        public BgRemovalProviderSettings(string apiKey_)
        {
            if (string.IsNullOrEmpty(apiKey_))
            {
                Debug.LogWarning("[AIProvider] BgRemovalProviderSettings: apiKey is null or empty");
            }
            ApiKey = apiKey_ ?? string.Empty;
        }

        public BgRemovalProviderSettings(string apiKey_, string baseUrl_) : this(apiKey_)
        {
            BaseUrl = baseUrl_;
        }

        private BgRemovalProviderSettings CloneInternal()
        {
            return new BgRemovalProviderSettings
            {
                ApiKey = ApiKey,
                BaseUrl = BaseUrl,
                DefaultModel = DefaultModel,
                TimeoutSeconds = TimeoutSeconds,
                MaxRetries = MaxRetries,
                CustomHeaders = new Dictionary<string, string>(CustomHeaders)
            };
        }
    }
}
