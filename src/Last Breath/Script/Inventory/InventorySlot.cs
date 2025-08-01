namespace LastBreath.Script.Inventory
{
    using System;
    using Contracts.Enums;
    using Godot;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Items;

    public partial class InventorySlot : BaseSlot<Item>
    {
        private const string UID = "uid://bqlqfsqoepfhs";
        private Label? _quantityLabel;
        private int _quantity;

        public event Action<Item, MouseButtonPressed>? OnClick;

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (ObservableProperty.SetProperty(ref _quantity, value))
                    UpdateQuantity();
            }
        }

        public override void _Ready()
        {
            _quantityLabel = GetNode<Label>("QuantityText");
            this.MouseEntered += OnMouseEnter;
            this.MouseExited += OnMouseExit;
            this.TextureNormal = DefaltTexture;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton p && CurrentItem != null)
            {
                OnClick?.Invoke(CurrentItem, MouseInputHelper.GetPressedButtons(p));
                GetViewport().SetInputAsHandled();
            }
        }

        public void AddNewItem(Item item)
        {
            CurrentItem = item;
            Quantity += item.Quantity;
            this.TextureNormal = item.Icon;
        }

        public bool RemoveItemStacks(int amount)
        {
            int canRemove = Mathf.Min(Quantity, amount);
            if (amount - canRemove > 0) return false;

            Quantity -= canRemove;

            return true;
        }

        public int AddItemStacks(int amount)
        {
            if (CurrentItem == null) return amount;

            int availableSpace = CurrentItem.MaxStackSize - Quantity;
            int addAmount = Mathf.Min(availableSpace, amount);
            Quantity += addAmount;

            return amount - addAmount;
        }

        public void ClearSlot()
        {
            CurrentItem = null;
            this.TextureNormal = DefaltTexture;
            _quantityLabel!.Text = string.Empty;
            Quantity = 0;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void UpdateQuantity()
        {
            if (Quantity < 1)
                ClearSlot();
            else
                _quantityLabel!.Text = SetQuantity();
        }

        private string SetQuantity() => Quantity > 1 ? Quantity.ToString() : string.Empty;
    }
}
