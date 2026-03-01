using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Weppy.AIProvider.Editor
{
    public class DragDropReorderController<TItemType>
    {
        private readonly VisualElement _itemList;
        private readonly Func<TItemType, bool> _isDraggableChecker;
        private readonly Action _onOrderChanged;
        private readonly Action<TItemType> _onDisabledItemClicked;

        private readonly Dictionary<VisualElement, TItemType> _entryToItemType = new Dictionary<VisualElement, TItemType>();
        private readonly List<TItemType> _itemOrder = new List<TItemType>();

        private VisualElement _draggedElement;
        private VisualElement _dropIndicator;
        private int _dragStartIndex = -1;

        public IReadOnlyList<TItemType> ItemOrder => _itemOrder;

        public DragDropReorderController(
            VisualElement itemList_,
            Func<TItemType, bool> isDraggableChecker_,
            Action onOrderChanged_,
            Action<TItemType> onDisabledItemClicked_ = null)
        {
            _itemList = itemList_;
            _isDraggableChecker = isDraggableChecker_;
            _onOrderChanged = onOrderChanged_;
            _onDisabledItemClicked = onDisabledItemClicked_;

            CreateDropIndicator();
            RegisterGlobalEvents();
        }

        public void Initialize(List<TItemType> itemOrder_)
        {
            _itemOrder.Clear();
            _itemOrder.AddRange(itemOrder_);
        }

        public void SetupEntry(VisualElement entry_, TItemType itemType_)
        {
            bool isDraggable = _isDraggableChecker?.Invoke(itemType_) ?? false;

            _entryToItemType[entry_] = itemType_;

            VisualElement dragHandle = new VisualElement();
            dragHandle.AddToClassList("reorder-drag-handle");
            dragHandle.Add(new Label("\u2195"));

            if (!isDraggable)
            {
                dragHandle.AddToClassList("reorder-drag-handle--disabled");
            }

            entry_.Insert(0, dragHandle);
            entry_.AddToClassList("reorder-item");

            if (!isDraggable)
            {
                entry_.AddToClassList("reorder-item--disabled");
                return;
            }

            entry_.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0 && dragHandle.worldBound.Contains(evt.position))
                {
                    StartDrag(entry_);
                    evt.StopPropagation();
                }
            });
        }

        public VisualElement CreateToggleWrapper(Toggle toggle_, TItemType itemType_, bool isEnabled_)
        {
            VisualElement toggleWrapper = new VisualElement();
            toggleWrapper.AddToClassList("reorder-toggle-wrapper");
            toggleWrapper.Add(toggle_);

            if (!isEnabled_)
            {
                toggle_.SetEnabled(false);
                toggleWrapper.RegisterCallback<ClickEvent>(evt =>
                {
                    _onDisabledItemClicked?.Invoke(itemType_);
                    evt.StopPropagation();
                });
            }

            return toggleWrapper;
        }

        public VisualElement CreateDropdownWrapper(DropdownField dropdown_, TItemType itemType_, bool isEnabled_)
        {
            VisualElement dropdownWrapper = new VisualElement();
            dropdownWrapper.AddToClassList("reorder-dropdown-wrapper");
            dropdownWrapper.Add(dropdown_);

            if (!isEnabled_)
            {
                dropdown_.SetEnabled(false);
                dropdown_.AddToClassList("reorder-dropdown--disabled");
                dropdownWrapper.RegisterCallback<ClickEvent>(evt =>
                {
                    _onDisabledItemClicked?.Invoke(itemType_);
                    evt.StopPropagation();
                });
            }

            return dropdownWrapper;
        }

        public void Clear()
        {
            _entryToItemType.Clear();
            _itemOrder.Clear();
            _draggedElement = null;
            _dragStartIndex = -1;
        }

        public void RebuildUI()
        {
            if (_itemList == null)
                return;

            _itemList.Clear();

            foreach (TItemType itemType in _itemOrder)
            {
                foreach (KeyValuePair<VisualElement, TItemType> kvp in _entryToItemType)
                {
                    if (EqualityComparer<TItemType>.Default.Equals(kvp.Value, itemType))
                    {
                        _itemList.Add(kvp.Key);
                        break;
                    }
                }
            }
        }

        public int GetPriorityForIndex(int index_, int basePriority_ = 100, int step_ = 10)
        {
            return basePriority_ - (index_ * step_);
        }

        private void CreateDropIndicator()
        {
            _dropIndicator = new VisualElement();
            _dropIndicator.AddToClassList("reorder-drop-indicator");
            _dropIndicator.style.display = DisplayStyle.None;
        }

        private void RegisterGlobalEvents()
        {
            _itemList.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_draggedElement != null)
                {
                    UpdateDrag(evt.position);
                    evt.StopPropagation();
                }
            });

            _itemList.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_draggedElement != null)
                {
                    EndDrag(evt.position);
                    evt.StopPropagation();
                }
            });
        }

        private void StartDrag(VisualElement entry_)
        {
            _draggedElement = entry_;
            _dragStartIndex = _itemList.IndexOf(entry_);
            entry_.AddToClassList("reorder-item--dragging");
            _itemList.CaptureMouse();
        }

        private void UpdateDrag(Vector2 position_)
        {
            if (_draggedElement == null || _itemList == null)
                return;

            int targetIndex = GetDropIndex(position_);

            if (_dropIndicator.parent != _itemList)
            {
                _itemList.Add(_dropIndicator);
            }

            _dropIndicator.style.display = DisplayStyle.Flex;

            if (targetIndex >= 0 && targetIndex < _itemList.childCount)
            {
                VisualElement targetElement = _itemList.ElementAt(targetIndex);
                if (targetElement != _dropIndicator && targetElement != _draggedElement)
                {
                    _itemList.Remove(_dropIndicator);
                    _itemList.Insert(targetIndex, _dropIndicator);
                }
            }
        }

        private void EndDrag(Vector2 position_)
        {
            if (_draggedElement == null)
                return;

            _draggedElement.RemoveFromClassList("reorder-item--dragging");
            _itemList.ReleaseMouse();

            int targetIndex = GetDropIndex(position_);

            _dropIndicator.style.display = DisplayStyle.None;
            if (_dropIndicator.parent == _itemList)
            {
                _itemList.Remove(_dropIndicator);
            }

            if (targetIndex >= 0 && targetIndex != _dragStartIndex)
            {
                ReorderItem(_dragStartIndex, targetIndex);
            }

            _draggedElement = null;
            _dragStartIndex = -1;
        }

        private int GetDropIndex(Vector2 position_)
        {
            if (_itemList == null)
                return -1;

            int index = 0;
            foreach (VisualElement child in _itemList.Children())
            {
                if (child == _dropIndicator || child == _draggedElement)
                {
                    continue;
                }

                Rect bounds = child.worldBound;
                float midY = bounds.y + bounds.height / 2;

                if (position_.y < midY)
                {
                    return index;
                }
                index++;
            }

            return index;
        }

        private void ReorderItem(int fromIndex_, int toIndex_)
        {
            if (fromIndex_ < 0 || fromIndex_ >= _itemOrder.Count)
                return;

            TItemType itemType = _itemOrder[fromIndex_];
            _itemOrder.RemoveAt(fromIndex_);

            if (toIndex_ > fromIndex_)
            {
                toIndex_--;
            }

            if (toIndex_ < 0)
                toIndex_ = 0;
            if (toIndex_ > _itemOrder.Count)
                toIndex_ = _itemOrder.Count;

            _itemOrder.Insert(toIndex_, itemType);

            _onOrderChanged?.Invoke();
            RebuildUI();
        }
    }
}
