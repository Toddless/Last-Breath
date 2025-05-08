namespace Playground.Script.Inventory
{
    using System;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Items;

    public partial class InventorySlot : Button
    {
        private const string UID = "uid://bqlqfsqoepfhs";
        private Label? _quantityLabel;
        private int _quantity;

        public event Action<Item, MouseButtonPressed>? OnClick;

        public Item? Item { get; private set; }

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
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton p && Item != null)
            {
                OnClick?.Invoke(Item, MouseInputHelper.GetPressedButtons(p));
                GetViewport().SetInputAsHandled();
            }
        }

        public void AddNewItem(Item item)
        {
            Item = item;
            Quantity += item.Quantity;
            Icon = item.Icon;
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
            if (Item == null) return amount;

            int availableSpace = Item.MaxStackSize - Quantity;
            int addAmount = Mathf.Min(availableSpace, amount);
            Quantity += addAmount;

            return amount - addAmount;
        }

        public void ClearSlot()
        {
            Item = null;
            Icon = null;
            _quantityLabel!.Text = string.Empty;
            Quantity = 0;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void UpdateQuantity()
        {
            if (Quantity < 1)
                ClearSlot();
            else
                _quantityLabel!.Text = Quantity.ToString();
        }
    }
}
