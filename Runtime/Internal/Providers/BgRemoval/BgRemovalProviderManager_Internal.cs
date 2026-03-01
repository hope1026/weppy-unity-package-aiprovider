using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Weppy.AIProvider
{
    public partial class BgRemovalProviderManager : IDisposable
    {
        private readonly List<BgRemovalProviderEntry> _providerEntries = new List<BgRemovalProviderEntry>();

        private readonly Dictionary<BgRemovalProviderType, BgRemovalProviderAbstract> _providers =
            new Dictionary<BgRemovalProviderType, BgRemovalProviderAbstract>();

        private bool _disposed;

        public BgRemovalProviderManager()
        {
        }

        public BgRemovalProviderManager(IEnumerable<BgRemovalProviderEntry> entries_)
        {
            if (entries_ != null)
            {
                foreach (BgRemovalProviderEntry entry in entries_)
                {
                    AddProviderInternal(entry);
                }
            }
        }

        private bool AddProviderInternal(BgRemovalProviderEntry entry_)
        {
            if (entry_ == null)
                return false;

            if (entry_.Settings == null)
                return false;

            BgRemovalProviderEntry existing = _providerEntries.FirstOrDefault(e => e.ProviderType == entry_.ProviderType);
            if (existing != null)
            {
                _providerEntries.Remove(existing);
                DisposeProvider(existing.ProviderType);
            }

            _providerEntries.Add(entry_);
            return true;
        }

        private bool AddProviderInternal(BgRemovalProviderType providerType_, BgRemovalProviderSettings settings_)
        {
            return AddProviderInternal(new BgRemovalProviderEntry(providerType_, settings_));
        }

        private bool RemoveProviderInternal(BgRemovalProviderType providerType_)
        {
            BgRemovalProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry == null)
                return false;

            _providerEntries.Remove(entry);
            DisposeProvider(providerType_);
            return true;
        }

        private void SetProviderEnabledInternal(BgRemovalProviderType providerType_, bool enabled_)
        {
            BgRemovalProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null)
            {
                entry.IsEnabled = enabled_;
            }
        }

        private void SetProviderDefaultModelInternal(BgRemovalProviderType providerType_, string model_)
        {
            BgRemovalProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            if (entry != null && entry.Settings != null)
            {
                entry.Settings.DefaultModel = model_;
                DisposeProvider(providerType_);
            }
        }

        private async Task<BgRemovalResponse> RemoveBackgroundInternalAsync(
            BgRemovalRequestParams requestParams_,
            CancellationToken cancellationToken_)
        {
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return BgRemovalResponse.FromError("Invalid params");

            List<BgRemovalRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_: true);

            if (targets.Count == 0)
                return BgRemovalResponse.FromError("No enabled providers available");

            Exception lastException = null;
            string lastError = null;

            foreach (BgRemovalRequestProviderTarget target in targets)
            {
                BgRemovalProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_: true))
                    continue;

                try
                {
                    BgRemovalProviderAbstract provider = GetOrCreateProvider(entry);
                    if (provider == null)
                        continue;

                    string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);

                    AIProviderLogger.Log($"[BgRemoval] Requesting {entry.ProviderType} (Model: {model}). Image Length: {requestParams_.RequestPayload.Base64Image?.Length ?? 0}");
                    AIProviderLogger.LogVerbose($"[BgRemoval] Request Payload: {UnityEngine.JsonUtility.ToJson(requestParams_.RequestPayload)}");

                    BgRemovalResponse response = await provider.RemoveBackgroundAsync(requestParams_.RequestPayload, model, cancellationToken_);

                    AIProviderLogger.Log($"[BgRemoval] Response from {entry.ProviderType}: Success={response.IsSuccess}, Error={response.ErrorMessage}");

                    if (response.IsSuccess)
                    {
                        response.ProviderType = entry.ProviderType;
                        return response;
                    }

                    lastError = response.ErrorMessage;
                }
                catch (OperationCanceledException)
                {
                    return BgRemovalResponse.FromError("Request was cancelled");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    lastError = ex.Message;
                }
            }

            return BgRemovalResponse.FromError(lastError ?? lastException?.Message ?? "All providers failed");
        }

        private async Task<Dictionary<BgRemovalProviderType, BgRemovalResponse>> RemoveBackgroundToAllProvidersInternalAsync(
            BgRemovalRequestParams requestParams_,
            bool onlyEnabled_,
            CancellationToken cancellationToken_)
        {
            Dictionary<BgRemovalProviderType, BgRemovalResponse> results =
                new Dictionary<BgRemovalProviderType, BgRemovalResponse>();
            if (requestParams_ == null || requestParams_.RequestPayload == null)
                return results;

            List<BgRemovalRequestProviderTarget> targets = GetOrderedTargets(requestParams_.Providers, onlyEnabled_);

            List<Task<KeyValuePair<BgRemovalProviderType, BgRemovalResponse>>> tasks =
                new List<Task<KeyValuePair<BgRemovalProviderType, BgRemovalResponse>>>();

            foreach (BgRemovalRequestProviderTarget target in targets)
            {
                BgRemovalProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == target.ProviderType);
                if (entry == null || !IsEntryAvailable(entry, onlyEnabled_))
                    continue;

                string model = ResolveModel(target.Model, requestParams_.RequestPayload.Model, entry.Settings?.DefaultModel);
                tasks.Add(RemoveBackgroundToProviderInternalAsync(entry, requestParams_.RequestPayload, model, cancellationToken_));
            }

            KeyValuePair<BgRemovalProviderType, BgRemovalResponse>[] responses = await Task.WhenAll(tasks);

            foreach (KeyValuePair<BgRemovalProviderType, BgRemovalResponse> kvp in responses)
            {
                results[kvp.Key] = kvp.Value;
            }

            return results;
        }

        private bool IsProviderAvailableInternal(BgRemovalProviderType providerType_)
        {
            BgRemovalProviderEntry entry = _providerEntries.FirstOrDefault(e => e.ProviderType == providerType_);
            return entry != null && entry.IsEnabled && !string.IsNullOrEmpty(entry.Settings?.ApiKey);
        }

        private List<BgRemovalProviderType> GetAvailableProviderTypesInternal()
        {
            return _providerEntries
                .Where(e => IsEntryAvailable(e, onlyEnabled_: true))
                .Select(e => e.ProviderType)
                .ToList();
        }

        private BgRemovalProviderType GetHighestPriorityProviderInternal()
        {
            BgRemovalProviderEntry entry = _providerEntries.FirstOrDefault(e => IsEntryAvailable(e, onlyEnabled_: true));
            return entry?.ProviderType ?? BgRemovalProviderType.NONE;
        }

        private void DisposeInternal()
        {
            if (_disposed)
                return;

            foreach (BgRemovalProviderAbstract provider in _providers.Values)
            {
                ((IDisposable)provider).Dispose();
            }

            _providers.Clear();
            _providerEntries.Clear();
            _disposed = true;
        }

        private async Task<KeyValuePair<BgRemovalProviderType, BgRemovalResponse>> RemoveBackgroundToProviderInternalAsync(
            BgRemovalProviderEntry entry_,
            BgRemovalRequestPayload requestPayload_,
            string model_,
            CancellationToken cancellationToken_)
        {
            try
            {
                BgRemovalProviderAbstract provider = GetOrCreateProvider(entry_);
                if (provider == null)
                    return new KeyValuePair<BgRemovalProviderType, BgRemovalResponse>(
                        entry_.ProviderType,
                        BgRemovalResponse.FromError($"Failed to create provider '{entry_.ProviderType}'"));

                AIProviderLogger.Log($"[BgRemoval] Requesting {entry_.ProviderType} (Model: {model_}). Image Length: {requestPayload_.Base64Image?.Length ?? 0}");
                AIProviderLogger.LogVerbose($"[BgRemoval] Request Payload: {UnityEngine.JsonUtility.ToJson(requestPayload_)}");

                BgRemovalResponse response = await provider.RemoveBackgroundAsync(requestPayload_, model_, cancellationToken_);

                AIProviderLogger.Log($"[BgRemoval] Response from {entry_.ProviderType}: Success={response.IsSuccess}, Error={response.ErrorMessage}");

                return new KeyValuePair<BgRemovalProviderType, BgRemovalResponse>(entry_.ProviderType, response);
            }
            catch (OperationCanceledException)
            {
                return new KeyValuePair<BgRemovalProviderType, BgRemovalResponse>(
                    entry_.ProviderType,
                    BgRemovalResponse.FromError("Request was cancelled"));
            }
            catch (Exception ex)
            {
                return new KeyValuePair<BgRemovalProviderType, BgRemovalResponse>(
                    entry_.ProviderType,
                    BgRemovalResponse.FromError(ex.Message));
            }
        }

        private List<BgRemovalRequestProviderTarget> GetOrderedTargets(List<BgRemovalRequestProviderTarget> targets_, bool onlyEnabled_)
        {
            List<BgRemovalRequestProviderTarget> targets = targets_ != null
                ? new List<BgRemovalRequestProviderTarget>(targets_)
                : new List<BgRemovalRequestProviderTarget>();

            if (targets.Count == 0)
            {
                foreach (BgRemovalProviderEntry entry in _providerEntries)
                {
                    if (!IsEntryAvailable(entry, onlyEnabled_))
                        continue;

                    targets.Add(new BgRemovalRequestProviderTarget
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

        private bool IsEntryAvailable(BgRemovalProviderEntry entry_, bool onlyEnabled_)
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

        private BgRemovalProviderAbstract GetOrCreateProvider(BgRemovalProviderEntry entry_)
        {
            if (_providers.TryGetValue(entry_.ProviderType, out BgRemovalProviderAbstract existingProvider))
            {
                return existingProvider;
            }

            try
            {
                BgRemovalProviderAbstract provider = BgRemovalProviderFactory.Create(entry_.ProviderType, entry_.Settings);
                _providers[entry_.ProviderType] = provider;
                return provider;
            }
            catch
            {
                return null;
            }
        }

        private void DisposeProvider(BgRemovalProviderType providerType_)
        {
            if (_providers.TryGetValue(providerType_, out BgRemovalProviderAbstract provider))
            {
                ((IDisposable)provider).Dispose();
                _providers.Remove(providerType_);
            }
        }
    }
}
