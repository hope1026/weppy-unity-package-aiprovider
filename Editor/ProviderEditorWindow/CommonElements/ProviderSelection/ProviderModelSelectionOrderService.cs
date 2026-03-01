using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class ProviderModelSelectionOrderService<TProviderType, TModelInfo>
        where TProviderType : struct, Enum
        where TModelInfo : class
    {
        private readonly ProviderModelSelectionConfig<TProviderType, TModelInfo> _config;

        public ProviderModelSelectionOrderService(ProviderModelSelectionConfig<TProviderType, TModelInfo> config_)
        {
            _config = config_ ?? throw new ArgumentNullException(nameof(config_));
        }

        public List<TProviderType> LoadProviderOrder(
            EditorProviderManagerInterface<TProviderType, TModelInfo> manager_,
            EditorDataStorage storage_)
        {
            List<TProviderType> defaultOrder = new List<TProviderType>();
            if (manager_ != null)
            {
                List<TProviderType> providers = manager_.GetProviders();
                foreach (TProviderType providerType in providers)
                {
                    if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                        continue;

                    defaultOrder.Add(providerType);
                }
            }
            else
            {
                TProviderType[] values = (TProviderType[])Enum.GetValues(typeof(TProviderType));
                foreach (TProviderType providerType in values)
                {
                    if (_config.IsNoneProvider != null && _config.IsNoneProvider(providerType))
                        continue;

                    defaultOrder.Add(providerType);
                }
            }

            if (storage_ == null)
                return defaultOrder;

            string savedOrder = storage_.GetString(_config.ProviderOrderStorageKey);
            if (string.IsNullOrEmpty(savedOrder))
                return defaultOrder;

            string[] orderParts = savedOrder.Split(',');
            List<TProviderType> loadedOrder = new List<TProviderType>();

            foreach (string part in orderParts)
            {
                if (Enum.TryParse(part.Trim(), out TProviderType providerType) && defaultOrder.Contains(providerType))
                {
                    loadedOrder.Add(providerType);
                }
            }

            foreach (TProviderType providerType in defaultOrder)
            {
                if (!loadedOrder.Contains(providerType))
                {
                    loadedOrder.Add(providerType);
                }
            }

            return loadedOrder;
        }

        public void SaveProviderOrder(
            DragDropReorderController<TProviderType> dragDropController_,
            EditorDataStorage storage_)
        {
            if (storage_ == null || dragDropController_ == null)
                return;

            string orderString = string.Join(",", dragDropController_.ItemOrder);
            storage_.SetString(_config.ProviderOrderStorageKey, orderString);
            storage_.Save();
        }

        public void UpdatePrioritiesFromOrder(
            DragDropReorderController<TProviderType> dragDropController_,
            Dictionary<TProviderType, IntegerField> providerPriorities_)
        {
            if (dragDropController_ == null)
                return;

            IReadOnlyList<TProviderType> providerOrder = dragDropController_.ItemOrder;
            for (int i = 0; i < providerOrder.Count; i++)
            {
                TProviderType providerType = providerOrder[i];
                int priority = dragDropController_.GetPriorityForIndex(i);

                if (providerPriorities_.TryGetValue(providerType, out IntegerField priorityField))
                {
                    priorityField.SetValueWithoutNotify(priority);
                }

                _config.SetProviderPriority?.Invoke(providerType, priority);
            }
        }

        public List<TProviderType> SortProvidersByEnabledState(
            List<TProviderType> providerOrder_,
            Func<TProviderType, bool> hasProviderAuth_,
            Func<TProviderType, bool> isProviderEnabled_)
        {
            List<(TProviderType providerType, int originalIndex, bool isEnabled)> providers =
                new List<(TProviderType, int, bool)>();

            for (int i = 0; i < providerOrder_.Count; i++)
            {
                TProviderType providerType = providerOrder_[i];
                bool hasProviderAuth = hasProviderAuth_ != null && hasProviderAuth_(providerType);
                bool isProviderEnabled = isProviderEnabled_ == null || isProviderEnabled_(providerType);
                bool isEnabled = hasProviderAuth && isProviderEnabled;
                providers.Add((providerType, i, isEnabled));
            }

            providers.Sort((a_, b_) =>
            {
                if (a_.isEnabled != b_.isEnabled)
                    return b_.isEnabled.CompareTo(a_.isEnabled);
                return a_.originalIndex.CompareTo(b_.originalIndex);
            });

            List<TProviderType> sortedOrder = new List<TProviderType>();
            foreach ((TProviderType providerType, int originalIndex, bool isEnabled) item in providers)
            {
                sortedOrder.Add(item.providerType);
            }

            return sortedOrder;
        }
    }
}
