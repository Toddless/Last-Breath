namespace Playground.Script.Inventory
{
    using Godot;
    using Playground.Script.Items;

    public partial class InventorySlot : Button
    {
        #region Private fields
        private Label? _quantityLabel;
        private Item? _item;
        private int _quantity;
        #endregion

        #region Properties
        public Item? Item
        {
            get => _item;
            set => _item = value;
        }

        #endregion

        public override void _Ready()
        {
            _quantityLabel = GetNode<Label>("QuantityText");
        }

        private void OnMouseEntered()
        {
        }

        private void OnMouseExited()
        {
        }

        private void OnPressed()
        {
            GD.Print("Pressed");
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
    }
}
