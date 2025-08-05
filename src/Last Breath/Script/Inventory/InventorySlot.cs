namespace LastBreath.Script.Inventory
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Godot;
    using LastBreath.Script.Helpers;

    public partial class InventorySlot : BaseSlot<IItem>
    {
        private const string UID = "uid://bqlqfsqoepfhs";
        [Export] private Label? _quantityLabel;
        private int _quantity;

        public event Action<IItem, MouseButtonPressed>? OnItemClicked;

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
            this.MouseEntered += OnMouseEnter;
            this.MouseExited += OnMouseExit;
            this.TextureNormal = DefaltTexture;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton p && CurrentItem != null)
            {
                OnItemClicked?.Invoke(CurrentItem, MouseInputHelper.GetPressedButtons(p));
                GetViewport().SetInputAsHandled();
            }
        }

        public void AddNewItem(IItem item)
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
