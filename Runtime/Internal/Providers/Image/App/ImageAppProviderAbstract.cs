using System;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    internal abstract class ImageAppProviderAbstract : IDisposable
    {
        protected ImageAppProviderSettings _settings;
        protected bool _disposed;

        internal abstract ImageAppProviderType ProviderType { get; }

        internal abstract Task<ImageResponse> GenerateImageAsync(
            ImageRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_ = default);

        protected ImageAppProviderAbstract(ImageAppProviderSettings settings_)
        {
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
