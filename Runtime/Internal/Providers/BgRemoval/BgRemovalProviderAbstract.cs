using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal abstract class BgRemovalProviderAbstract : IDisposable
    {
        protected HttpClientWrapper _httpClient;
        protected BgRemovalProviderSettings _settings;
        protected bool _disposed;
        
        internal abstract BgRemovalProviderType ProviderType { get; }
        internal abstract Task<BgRemovalResponse> RemoveBackgroundAsync(BgRemovalRequestPayload requestPayload_, string model_, CancellationToken cancellationToken_ = default);

        protected BgRemovalProviderAbstract(BgRemovalProviderSettings settings_, string defaultBaseUrl_)
        {
            if (settings_ == null)
            {
                Debug.LogError("BgRemovalProvider: Null settings");
                return;
            }

            _settings = settings_;

            if (string.IsNullOrEmpty(_settings.BaseUrl))
                _settings.BaseUrl = defaultBaseUrl_;

            _httpClient = new HttpClientWrapper(_settings.TimeoutSeconds);
        }
        
        void IDisposable.Dispose()
        {
            if (_disposed)
                return;

            ((IDisposable)_httpClient)?.Dispose();
            _disposed = true;
        }
    }
}
