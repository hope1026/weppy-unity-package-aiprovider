using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Weppy.AIProvider
{
    internal abstract class ChatCliProviderAbstract : IDisposable
    {
        protected ChatCliProviderSettings _settings;
        protected bool _disposed;

        internal abstract ChatCliProviderType ProviderType { get; }

        internal abstract Task<ChatCliResponse> SendMessageAsync(
            ChatCliRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default);

        internal virtual bool SupportsPersistentSession => false;

        internal virtual Task<ChatCliResponse> SendPersistentMessageAsync(
            ChatCliRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default)
        {
            return SendMessageAsync(requestPayload_, model_, cancellationToken_);
        }

        internal virtual void ResetSession()
        {
        }

        protected ChatCliProviderAbstract(ChatCliProviderSettings settings_)
        {
            if (settings_ == null)
            {
                Debug.LogError("ChatCliProvider: Null settings");
                return;
            }

            _settings = settings_;
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
                return;

            DisposeInternal();
            _disposed = true;
        }

        protected virtual void DisposeInternal()
        {
        }
    }
}
