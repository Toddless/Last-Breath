namespace LastBreath.Inventory
{
    using System;
    using Core;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Godot;
    using Godot.Collections;

    public abstract partial class Slot : Control
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
                RefreshUi();
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
                RefreshUi();
            }
        }

        public event Action<string>? ItemDeleted;

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (CurrentItem == null) return new Variant();

            var payload = new Dictionary
            {
                [DragPayload.Item] = CurrentItem.ItemId,
                [DragPayload.Instance] = CurrentItem.InstanceId,
                [DragPayload.Quantity] = Quantity,
                [DragPayload.MaxStackSize] = CurrentItem.MaxStackSize,
                [DragPayload.Source] = GetPath()
            };


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
            if (data.VariantType != Variant.Type.Dictionary) return false;
            var payload = data.AsGodotDictionary();
            return payload.ContainsKey(DragPayload.Item) || payload.ContainsKey(DragPayload.EquipmentPiece);
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            var payload = data.AsGodotDictionary();
            if (payload.ContainsKey(DragPayload.EquipmentPiece))
            {
                OnEquipmentDropped((EquipmentPiece)payload[DragPayload.EquipmentPiece].AsInt32());
                return;
            }

            var itemId = payload[DragPayload.Item].AsString();
            var sourcePath = payload[DragPayload.Source].AsNodePath();
            var stackSize = payload[DragPayload.MaxStackSize].AsInt32();

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

        /// <summary>An equipped piece was dragged into this slot; the bag slot raises it up to the window.</summary>
        protected virtual void OnEquipmentDropped(EquipmentPiece piece)
        {
        }

        public virtual void ClearSlot(bool itemDeleted = false)
        {
            if (CurrentItem != null && itemDeleted)
                ItemDeleted?.Invoke(CurrentItem.InstanceId);
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

        protected virtual void RefreshUi()
        {
            Icon?.Texture = CurrentItem == null ? null : GetItemIcon?.Invoke(CurrentItem.InstanceId);
        }
    }
}
