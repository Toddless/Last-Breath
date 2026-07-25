namespace Crafting.Internal.Inventory
{
    using System;
    using Core;
    using Core.Items;
    using Godot;
    using Godot.Collections;

    internal abstract partial class Slot : Control
    {
        private int _quantity;
        [Export] protected TextureRect? Background;
        [Export] protected TextureRect? Icon;
        [Export] protected TextureRect? Frame;

        public Func<string, Texture2D?>? GetItemIcon;

        public ItemInstance? CurrentItem
        {
            get;
            private set
            {
                if (field == value) return;
                field = value;
                RefreshUI();
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value) return;
                _quantity = value;
                if (_quantity <= 0) ClearSlot(true);
                RefreshUI();
            }
        }

        public event Action<string>? ItemRemovedFromSlot;

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (CurrentItem == null) return new Variant();

            var payload = new Dictionary { ["Item"] = CurrentItem.ItemId, ["Quantity"] = Quantity, ["MaxStackSize"] = CurrentItem.MaxStackSize, ["Source"] = GetPath() };


            var preview = new TextureRect
            {
                Texture = Icon?.Texture,
                MouseFilter = MouseFilterEnum.Ignore,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                CustomMinimumSize = new Vector2(60, 60)
            };

            SetDragPreview(preview);
            return payload;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            return data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary().ContainsKey("Item");
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            var payload = data.AsGodotDictionary();
            var itemId = payload["Item"].AsString();
            var sourcePath = payload["Source"].AsNodePath();
            var stackSize = payload["MaxStackSize"].AsInt32();

            var source = GetNodeOrNull<Slot>(sourcePath);
            if (source == null)
            {
                Tracker.TrackNotFound(sourcePath, this);
                return;
            }

            if (CurrentItem == null)
                MoveItems(source, this);
            else
            {
                if (stackSize == 0)
                {
                    Tracker.TrackNotFound(itemId, this);
                    return;
                }

                SwapItems(source, this);
            }
        }

        public virtual void ClearSlot(bool itemDeleted = false)
        {
            if (CurrentItem != null && itemDeleted)
                ItemRemovedFromSlot?.Invoke(CurrentItem.InstanceId);
            CurrentItem = null;
            Quantity = 0;
        }

        public virtual void SetItem(ItemInstance instance, int amount = 1)
        {
            CurrentItem = instance;
            Quantity = amount;
        }

        public virtual void MoveItems(Slot from, Slot to)
        {
            to.SetItem(from.CurrentItem!, from.Quantity);
            from.CurrentItem = null;
            from.Quantity = 0;
        }

        public virtual void SwapItems(Slot from, Slot to)
        {
            var itemFrom = from.CurrentItem;
            var quantityFrom = from.Quantity;

            var itemTo = to.CurrentItem;
            var quantityTo = to.Quantity;

            // if we can swap items, current item cannot be null (for both slots)
            from.SetItem(itemTo!, quantityTo);
            to.SetItem(itemFrom!, quantityFrom);
        }


        public virtual bool TryAddStacks(int amount, out int leftover)
        {
            if (CurrentItem == null)
            {
                leftover = amount;
                return false;
            }

            int available = CurrentItem.MaxStackSize - Quantity;
            int toAdd = Mathf.Min(available, amount);
            Quantity += toAdd;
            leftover = amount - toAdd;
            return toAdd > 0;
        }

        public virtual bool TryRemoveItemStacks(int amount = 1)
        {
            if (CurrentItem == null) return false;
            int toRemove = Mathf.Min(Quantity, amount);
            if (toRemove < amount) return false;

            Quantity -= toRemove;
            return true;
        }

        protected virtual void RefreshUI()
        {
            Icon?.Texture = CurrentItem == null ? null : GetItemIcon?.Invoke(CurrentItem.InstanceId);
        }
    }
}
