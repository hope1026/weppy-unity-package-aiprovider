using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider.Chat
{
    internal abstract class ChatProviderAbstract : IDisposable
    {
        protected HttpClientWrapper _httpClient;
        protected ChatProviderSettings _settings;
        protected bool _disposed;

        internal abstract ChatProviderType ProviderType { get; }

        internal abstract Task<ChatResponse> SendMessageAsync(ChatRequestPayload requestPayload_, string model_, CancellationToken cancellationToken_ = default);
        internal abstract Task StreamMessageAsync(ChatRequestPayload requestPayload_, string model_, Func<string, Task> onChunkReceived_, CancellationToken cancellationToken_ = default);

        protected ChatProviderAbstract(ChatProviderSettings settings_, string defaultBaseUrl_)
        {
            if (settings_ == null)
            {
                Debug.LogError("ChatProvider: Null settings");
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
