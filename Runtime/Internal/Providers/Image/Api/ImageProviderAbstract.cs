using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal abstract class ImageProviderAbstract : IDisposable
    {
        protected HttpClientWrapper _httpClient;
        protected ImageProviderSettings _settings;
        protected bool _disposed;

        internal abstract ImageProviderType ProviderType { get; }

        internal abstract Task<ImageResponse> GenerateImageAsync(ImageRequestPayload requestPayload_, string model_, CancellationToken cancellationToken_ = default);

        protected ImageProviderAbstract(ImageProviderSettings settings_, string defaultBaseUrl_)
        {
            if (settings_ == null)
            {
                Debug.LogError("ImageProvider: Null settings");
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
