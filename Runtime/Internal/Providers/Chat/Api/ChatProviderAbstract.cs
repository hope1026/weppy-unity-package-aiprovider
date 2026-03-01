using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal abstract class ChatProviderAbstract : IDisposable
    {
        protected HttpClientWrapper _httpClient;
        protected ChatProviderSettings _settings;
        protected bool _disposed;

        internal abstract ChatProviderType ProviderType { get; }

        internal abstract Task<ChatResponse> SendMessageAsync(ChatRequestPayload requestPayload_, string model_, CancellationToken cancellationToken_ = default);
        internal abstract Task StreamMessageAsync(ChatRequestPayload requestPayload_, string model_, Func<string, Task> onChunkReceived_, CancellationToken cancellationToken_ = default);

        protected static bool IsImageContentPart(string type_)
        {
            return string.Equals(type_, "image", StringComparison.Ordinal) ||
                   string.Equals(type_, "image_url", StringComparison.Ordinal);
        }

        protected static string BuildDataUrl(string mediaType_, string base64Data_)
        {
            if (string.IsNullOrEmpty(base64Data_))
                return null;

            string mediaType = string.IsNullOrEmpty(mediaType_) ? "image/png" : mediaType_;
            return $"data:{mediaType};base64,{base64Data_}";
        }

        protected static bool TryExtractSseData(string line_, out string data_)
        {
            data_ = null;
            if (string.IsNullOrEmpty(line_))
                return false;

            const string prefix = "data:";
            if (!line_.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            data_ = line_.Length > prefix.Length && line_[prefix.Length] == ' '
                ? line_.Substring(prefix.Length + 1)
                : line_.Substring(prefix.Length);
            return true;
        }

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
