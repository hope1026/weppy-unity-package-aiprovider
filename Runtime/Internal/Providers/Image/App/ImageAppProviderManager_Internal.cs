using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    public partial class ImageAppProviderManager : IDisposable
    {
        private readonly List<ImageAppProviderEntry> _providerEntries = new List<ImageAppProviderEntry>();
        private readonly Dictionary<ImageAppProviderType, ImageAppProviderAbstract> _providers =
            new Dictionary<ImageAppProviderType, ImageAppProviderAbstract>();
        private bool _disposed;

        public ImageAppProviderManager()
        {
        }

        public ImageAppProviderManager(IEnumerable<ImageAppProviderEntry> entries_)
        {
            if (entries_ != null)
            {
                foreach (ImageAppProviderEntry entry in entries_)
                {
                    AddProviderInternal(entry);
                }
            }
        }

        private bool AddProviderInternal(ImageAppProviderEntry entry_)
        {
            if (entry_ == null || entry_.Settings == null)
                return false;

            ImageAppProviderEntry existing = _providerEntries.FirstOrDefault(e => e.ProviderType == entry_.ProviderType);
            if (existing != null)
            {
                _providerEntries.Remove(existing);
                DisposeProvider(existing.ProviderType);
            }

            _providerEntries.Add(entry_);
            return true;
        }

        private bool AddProviderInternal(ImageAppProviderType providerType_, ImageAppProviderSettings settings_)
        {
            return AddProviderInternal(new ImageAppProviderEntry(providerType_, settings_));
        }

        private bool RemoveProviderInternal(ImageAppProviderType providerType_)
        {
            ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry == null)
                return false;

            _providerEntries.Remove(entry);
            DisposeProvider(providerType_);
            return true;
        }

        private void SetProviderEnabledInternal(ImageAppProviderType providerType_, bool enabled_)
        {
            ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null)
            {
                entry.IsEnabled = enabled_;
            }
        }

        private void SetProviderDefaultModelInternal(ImageAppProviderType providerType_, string model_)
        {
            ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null && entry.Settings != null)
            {
                entry.Settings.DefaultModel = model_;
                DisposeProvider(providerType_);
            }
        }

        private async Task<ImageResponse> GenerateImageInternalAsync(
            ImageAppRequestParams requestParams_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return ImageResponse.FromError("Invalid params");

            List<ImageAppRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);
            if (targets.Count == 0)
                return ImageResponse.FromError("No enabled providers available");

            Exception lastException = null;
            string lastError = null;

            foreach (ImageAppRequestProviderTarget target in targets)
            {
                ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                try
                {
                    ImageAppProviderAbstract provider = GetOrCreateProvider(entry);
                    if (provider == null)
                        continue;

                    string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                    AIProviderLogger.Log($"[Image App] Requesting {entry.ProviderType} (Model: {model}): {requestParams_.RequestPayload.Prompt}");
                    ImageResponse response = await provider.GenerateImageAsync(requestParams_.RequestPayload, model, cancellationToken_);
                    AIProviderLogger.Log($"[Image App] Response from {entry.ProviderType}: Success={response.IsSuccess}, Error={response.ErrorMessage}");

                    if (response.IsSuccess)
                    {
                        response.ProviderType = ImageProviderType.NONE;
                        return response;
                    }

                    lastError = response.ErrorMessage;
                }
                catch (OperationCanceledException)
                {
                    return ImageResponse.FromError("Request was cancelled");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    lastError = ex.Message;
                }
            }

            return ImageResponse.FromError(lastError ?? lastException?.Message ?? "All providers failed");
        }

        private async Task<Dictionary<ImageAppProviderType, ImageResponse>> GenerateImageToAllProvidersInternalAsync(
            ImageAppRequestParams requestParams_,
            bool onlyEnabled_,
            CancellationToken cancellationToken_)
        {
            Dictionary<ImageAppProviderType, ImageResponse> results = new Dictionary<ImageAppProviderType, ImageResponse>();
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return results;

            List<ImageAppRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_);
            List<Task<KeyValuePair<ImageAppProviderType, ImageResponse>>> tasks =
                new List<Task<KeyValuePair<ImageAppProviderType, ImageResponse>>>();

            foreach (ImageAppRequestProviderTarget target in targets)
            {
                ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);
                tasks.Add(GenerateToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_));
            }

            KeyValuePair<ImageAppProviderType, ImageResponse>[] responses = await Task.WhenAll(tasks);
            foreach (KeyValuePair<ImageAppProviderType, ImageResponse> kvp in responses)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        private async Task GenerateImageToAllProvidersInternalAsync(
            ImageAppRequestParams requestParams_,
            Action<ImageAppProviderType, ImageResponse> onProviderCompleted_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return;

            List<ImageAppRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);
            List<Task> tasks = new List<Task>();

            foreach (ImageAppRequestProviderTarget target in targets)
            {
                ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                Task task = Task.Run(async () =>
                {
                    KeyValuePair<ImageAppProviderType, ImageResponse> result =
                        await GenerateToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_);
                    onProviderCompleted_?.Invoke(result.Key, result.Value);
                }, cancellationToken_);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private bool IsProviderAvailableInternal(ImageAppProviderType providerType_)
        {
            ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            return entry != null && entry.IsEnabled && HasProviderAuth(entry.Settings);
        }

        private List<ImageAppProviderType> GetAvailableProviderTypesInternal()
        {
            return _providerEntries
                .Where(entry => IsEntryAvailable(entry, onlyEnabled_: true))
                .Select(e => e.ProviderType)
                .ToList();
        }

        private ImageAppProviderType GetHighestPriorityProviderInternal()
        {
            ImageAppProviderEntry entry = _providerEntries.FirstOrDefault(e => IsEntryAvailable(e, onlyEnabled_: true));
            return entry != null ? entry.ProviderType : ImageAppProviderType.NONE;
        }

        private void DisposeInternal()
        {
            if (_disposed)
                return;

            foreach (ImageAppProviderAbstract provider in _providers.Values)
            {
                if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _providers.Clear();
            _providerEntries.Clear();
            _disposed = true;
        }

        private async Task<KeyValuePair<ImageAppProviderType, ImageResponse>> GenerateToProviderInternalAsync(
            ImageAppProviderEntry entry_,
            ImageRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_)
        {
            try
            {
                ImageAppProviderAbstract provider = GetOrCreateProvider(entry_);
                if (provider == null)
                {
                    return new KeyValuePair<ImageAppProviderType, ImageResponse>(
                        entry_.ProviderType,
                        ImageResponse.FromError($"Failed to create app provider '{entry_.ProviderType}'"));
                }

                ImageResponse response = await provider.GenerateImageAsync(requestPayload_, model_, cancellationToken_);
                return new KeyValuePair<ImageAppProviderType, ImageResponse>(entry_.ProviderType, response);
            }
            catch (OperationCanceledException)
            {
                return new KeyValuePair<ImageAppProviderType, ImageResponse>(
                    entry_.ProviderType,
                    ImageResponse.FromError("Request was cancelled"));
            }
            catch (Exception ex)
            {
                return new KeyValuePair<ImageAppProviderType, ImageResponse>(
                    entry_.ProviderType,
                    ImageResponse.FromError(ex.Message));
            }
        }

        private List<ImageAppRequestProviderTarget> GetOrderedTargets(List<ImageAppRequestProviderTarget> targets_, bool onlyEnabled_)
        {
            List<ImageAppRequestProviderTarget> targets = targets_ != null
                ? new List<ImageAppRequestProviderTarget>(targets_)
                : new List<ImageAppRequestProviderTarget>();

            if (targets.Count == 0)
            {
                foreach (ImageAppProviderEntry entry in _providerEntries)
                {
                    if (!IsEntryAvailable(entry, onlyEnabled_))
                        continue;

                    targets.Add(new ImageAppRequestProviderTarget
                    {
                        ProviderType = entry.ProviderType,
                        Model = null,
                        Priority = 0
                    });
                }
            }

            return targets
                .Select((target, index) => new { target, index })
                .OrderByDescending(pair => pair.target.Priority)
                .ThenBy(pair => pair.index)
                .Select(pair => pair.target)
                .ToList();
        }

        private bool IsEntryAvailable(ImageAppProviderEntry entry_, bool onlyEnabled_)
        {
            if (entry_ == null)
                return false;

            if (onlyEnabled_ && !entry_.IsEnabled)
                return false;

            return HasProviderAuth(entry_.Settings);
        }

        private bool HasProviderAuth(ImageAppProviderSettings settings_)
        {
            if (settings_ == null)
                return false;

            if (!settings_.UseApiKey)
                return true;

            return !string.IsNullOrEmpty(settings_.ApiKey);
        }

        private string ResolveModel(string targetModel_, string payloadModel_, string defaultModel_)
        {
            if (!string.IsNullOrEmpty(targetModel_))
                return targetModel_;

            if (!string.IsNullOrEmpty(payloadModel_))
                return payloadModel_;

            return defaultModel_;
        }

        private ImageAppProviderAbstract GetOrCreateProvider(ImageAppProviderEntry entry_)
        {
            if (_providers.TryGetValue(entry_.ProviderType, out ImageAppProviderAbstract existingProvider))
            {
                return existingProvider;
            }

            try
            {
                ImageAppProviderAbstract provider = ImageAppProviderFactory.Create(entry_.ProviderType, entry_.Settings);
                if (provider == null)
                    return null;

                _providers[entry_.ProviderType] = provider;
                return provider;
            }
            catch
            {
                return null;
            }
        }

        private void DisposeProvider(ImageAppProviderType providerType_)
        {
            if (_providers.TryGetValue(providerType_, out ImageAppProviderAbstract provider))
            {
                ((IDisposable)provider).Dispose();
                _providers.Remove(providerType_);
            }
        }
    }
}
