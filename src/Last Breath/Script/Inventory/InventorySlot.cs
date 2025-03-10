namespace Playground.Script.Inventory
{
    using Godot;
    using Playground.Script.Items;

    public partial class InventorySlot : Button
    {
        private const string UID = "uid://bqlqfsqoepfhs";
        private Label? _quantityLabel;
        private Item? _item;
        private int _quantity;

        public Item? Item
        {
            get => _item;
            set => _item = value;
        }

        public override void _Ready()
        {
            _quantityLabel = GetNode<Label>("QuantityText");
        }

        public void SetItem(Item? item)
        {
            if (item != null)
            {
                Item = item;
                _quantity += item.Quantity;
                Icon = item.Icon;
            }
            else
            {
                Item = null;
                Icon = null;
            }
            UpdateQuantity();
        }

        public void AddItem(Item item)
        {
            _quantity += item.Quantity;
            UpdateQuantity();
        }

        public void RemoveItem(Item item)
        {
            _quantity -= item.Quantity;
            UpdateQuantity();

            if (_quantity == 0)
            {
                SetItem(null);
            }
        }

        public void UpdateQuantity()
        {
            if (_quantity <= 1)
                _quantityLabel!.Text = string.Empty;
            else
                _quantityLabel!.Text = _quantity.ToString();
        }

        public void Clear()
        {
            this.Item = null;
            this.Icon = null;
            this._quantityLabel!.Text = string.Empty;
            _quantity = 0;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
