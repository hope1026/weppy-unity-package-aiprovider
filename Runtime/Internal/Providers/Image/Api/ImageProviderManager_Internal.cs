using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    public partial class ImageProviderManager : IDisposable
    {
        private readonly List<ImageProviderEntry> _providerEntries = new List<ImageProviderEntry>();
        private readonly Dictionary<ImageProviderType, ImageProviderAbstract> _providers =
            new Dictionary<ImageProviderType, ImageProviderAbstract>();
        private bool _disposed;

        public ImageProviderManager()
        {
        }

        public ImageProviderManager(IEnumerable<ImageProviderEntry> entries_)
        {
            if (entries_ != null)
            {
                foreach (ImageProviderEntry entry in entries_)
                {
                    AddProviderInternal(entry);
                }
            }
        }

        private bool AddProviderInternal(ImageProviderEntry entry_)
        {
            if (entry_ == null)
                return false;

            if (entry_.Settings == null)
                return false;

            ImageProviderEntry existing = _providerEntries.FirstOrDefault(e_ => e_.ProviderType == entry_.ProviderType);
            if (existing != null)
            {
                _providerEntries.Remove(existing);
                DisposeProvider(existing.ProviderType);
            }

            _providerEntries.Add(entry_);
            return true;
        }

        private bool AddProviderInternal(ImageProviderType providerType_, ImageProviderSettings settings_)
        {
            return AddProviderInternal(new ImageProviderEntry(providerType_, settings_));
        }

        private bool RemoveProviderInternal(ImageProviderType providerType_)
        {
            ImageProviderEntry entry = _providerEntries.FirstOrDefault(e_ => e_.ProviderType == providerType_);
            if (entry == null)
                return false;

            _providerEntries.Remove(entry);
            DisposeProvider(providerType_);
            return true;
        }

        private void SetProviderEnabledInternal(ImageProviderType providerType_, bool enabled_)
        {
            ImageProviderEntry entry = _providerEntries.FirstOrDefault(e_ => e_.ProviderType == providerType_);
            if (entry != null)
            {
                entry.IsEnabled = enabled_;
            }
        }

        private void SetProviderDefaultModelInternal(ImageProviderType providerType_, string model_)
        {
            ImageProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null && entry.Settings != null)
            {
                entry.Settings.DefaultModel = model_;
                DisposeProvider(providerType_);
            }
        }

        private async Task<ImageResponse> GenerateImageInternalAsync(
            ImageRequestParams requestParams_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return ImageResponse.FromError("Invalid params");

            List<ImageRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            if (targets.Count == 0)
                return ImageResponse.FromError("No enabled providers available");

            Exception lastException = null;
            string lastError = null;

            foreach (ImageRequestProviderTarget target in targets)
            {
                ImageProviderEntry entry = _providerEntries.FirstOrDefault(e_ => e_.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                try
                {
                    ImageProviderAbstract provider = GetOrCreateProvider(entry);
                    if (provider == null)
                        continue;

                    string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                    AIProviderLogger.Log($"[Image] Requesting {entry.ProviderType} (Model: {model}): {requestParams_.RequestPayload.Prompt}");
                    AIProviderLogger.LogVerbose($"[Image] Request Payload: {UnityEngine.JsonUtility.ToJson(requestParams_.RequestPayload)}");

                    ImageResponse response = await provider.GenerateImageAsync(requestParams_.RequestPayload, model, cancellationToken_);

                    AIProviderLogger.Log($"[Image] Response from {entry.ProviderType}: Success={response.IsSuccess}, Error={response.ErrorMessage}");

                    if (response.IsSuccess)
                    {
                        response.ProviderType = entry.ProviderType;
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

        private async Task<Dictionary<ImageProviderType, ImageResponse>> GenerateImageToAllProvidersInternalAsync(
            ImageRequestParams requestParams_,
            bool onlyEnabled_,
            CancellationToken cancellationToken_)
        {
            Dictionary<ImageProviderType, ImageResponse> results = new Dictionary<ImageProviderType, ImageResponse>();
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return results;

            List<ImageRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_);

            List<Task<KeyValuePair<ImageProviderType, ImageResponse>>> tasks =
                new List<Task<KeyValuePair<ImageProviderType, ImageResponse>>>();

            foreach (ImageRequestProviderTarget target in targets)
            {
                ImageProviderEntry entry = _providerEntries.FirstOrDefault(e_ => e_.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);
                tasks.Add(GenerateToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_));
            }

            KeyValuePair<ImageProviderType, ImageResponse>[] responses = await Task.WhenAll(tasks);

            foreach (KeyValuePair<ImageProviderType, ImageResponse> kvp in responses)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        private async Task GenerateImageToAllProvidersInternalAsync(
            ImageRequestParams requestParams_,
            Action<ImageProviderType, ImageResponse> onProviderCompleted_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return;

            List<ImageRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            List<Task> tasks = new List<Task>();

            foreach (ImageRequestProviderTarget target in targets)
            {
                ImageProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                Task task = Task.Run(async () =>
                {
                    KeyValuePair<ImageProviderType, ImageResponse> result =
                        await GenerateToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_);

                    if (onProviderCompleted_ != null)
                    {
                        try
                        {
                            onProviderCompleted_.Invoke(result.Key, result.Value);
                        }
                        catch (Exception callbackException)
                        {
                            AIProviderLogger.LogError(
                                $"[Image] Provider callback failed - Provider: {result.Key}, Error: {callbackException.Message}");
                        }
                    }
                }, cancellationToken_);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private bool IsProviderAvailableInternal(ImageProviderType providerType_)
        {
            ImageProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            return entry != null && entry.IsEnabled && !string.IsNullOrEmpty(entry.Settings?.ApiKey);
        }

        private List<ImageProviderType> GetAvailableProviderTypesInternal()
        {
            return _providerEntries
                .Where(entry_ => IsEntryAvailable(entry_, onlyEnabled_: true))
                .Select(e_ => e_.ProviderType)
                .ToList();
        }

        private ImageProviderType GetHighestPriorityProviderInternal()
        {
            ImageProviderEntry entry = _providerEntries.FirstOrDefault(e_ => IsEntryAvailable(e_, onlyEnabled_: true));
            return entry != null ? entry.ProviderType : ImageProviderType.NONE;
        }

        private void DisposeInternal()
        {
            if (_disposed)
                return;

            foreach (ImageProviderAbstract provider in _providers.Values)
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

        private async Task<KeyValuePair<ImageProviderType, ImageResponse>> GenerateToProviderInternalAsync(
            ImageProviderEntry entry_,
            ImageRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_)
        {
            try
            {
                ImageProviderAbstract provider = GetOrCreateProvider(entry_);
                if (provider == null)
                    return new KeyValuePair<ImageProviderType, ImageResponse>(
                        entry_.ProviderType,
                        ImageResponse.FromError($"Failed to create provider '{entry_.ProviderType}'"));

                AIProviderLogger.Log($"[Image] Requesting {entry_.ProviderType} (Model: {model_}): {requestPayload_.Prompt}");
                AIProviderLogger.LogVerbose($"[Image] Request Payload: {UnityEngine.JsonUtility.ToJson(requestPayload_)}");

                ImageResponse response = await provider.GenerateImageAsync(requestPayload_, model_, cancellationToken_);

                AIProviderLogger.Log($"[Image] Response from {entry_.ProviderType}: Success={response.IsSuccess}, Error={response.ErrorMessage}");

                return new KeyValuePair<ImageProviderType, ImageResponse>(entry_.ProviderType, response);
            }
            catch (OperationCanceledException)
            {
                return new KeyValuePair<ImageProviderType, ImageResponse>(
                    entry_.ProviderType,
                    ImageResponse.FromError("Request was cancelled"));
            }
            catch (Exception ex)
            {
                return new KeyValuePair<ImageProviderType, ImageResponse>(
                    entry_.ProviderType,
                    ImageResponse.FromError(ex.Message));
            }
        }

        private List<ImageRequestProviderTarget> GetOrderedTargets(List<ImageRequestProviderTarget> targets_, bool onlyEnabled_)
        {
            List<ImageRequestProviderTarget> targets = targets_ != null
                ? new List<ImageRequestProviderTarget>(targets_)
                : new List<ImageRequestProviderTarget>();

            if (targets.Count == 0)
            {
                foreach (ImageProviderEntry entry in _providerEntries)
                {
                    if (!IsEntryAvailable(entry, onlyEnabled_))
                        continue;

                    targets.Add(new ImageRequestProviderTarget
                    {
                        ProviderType = entry.ProviderType,
                        Model = null,
                        Priority = 0
                    });
                }
            }

            return targets
                .Select((target_, index_) => new { target = target_, index = index_ })
                .OrderByDescending(pair_ => pair_.target.Priority)
                .ThenBy(pair_ => pair_.index)
                .Select(pair_ => pair_.target)
                .ToList();
        }

        private bool IsEntryAvailable(ImageProviderEntry entry_, bool onlyEnabled_)
        {
            if (entry_ == null)
                return false;

            if (onlyEnabled_ && !entry_.IsEnabled)
                return false;

            return !string.IsNullOrEmpty(entry_.Settings?.ApiKey);
        }

        private string ResolveModel(string targetModel_, string payloadModel_, string defaultModel_)
        {
            if (!string.IsNullOrEmpty(targetModel_))
                return targetModel_;

            if (!string.IsNullOrEmpty(payloadModel_))
                return payloadModel_;

            return defaultModel_;
        }

        private ImageProviderAbstract GetOrCreateProvider(ImageProviderEntry entry_)
        {
            if (_providers.TryGetValue(entry_.ProviderType, out ImageProviderAbstract existingProvider))
            {
                return existingProvider;
            }

            try
            {
                ImageProviderAbstract provider = ImageProviderFactory.Create(entry_.ProviderType, entry_.Settings);
                _providers[entry_.ProviderType] = provider;
                return provider;
            }
            catch
            {
                return null;
            }
        }

        private void DisposeProvider(ImageProviderType providerType_)
        {
            if (_providers.TryGetValue(providerType_, out ImageProviderAbstract provider))
            {
                ((IDisposable)provider).Dispose();
                _providers.Remove(providerType_);
            }
        }
    }
}
