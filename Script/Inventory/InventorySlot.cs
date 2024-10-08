namespace Playground
{
    using Godot;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class InventorySlot : Node
    {
        private Label _quantityLabel;
        private TextureRect _icon;
        private int _quantity;
        private int _index;
        public Inventory _inventory;
        public Item InventoryItem;

        public Inventory Inventory
        {
            get => _inventory;
            private set => _inventory = value;
        }

        public int Quantity
        {
            get => _quantity;
            private set => _quantity = value;
        }

        public override void _Ready()
        {
            _quantityLabel = GetNode<Label>("QuantityText");
            _icon = GetNode<TextureRect>("Icon");
        }

        public void OnMouseEntered()
        {
            if (InventoryItem != null)
            {
                _inventory.InfoText.Text = InventoryItem.ItemName;
            }
            else
            {
                _inventory.InfoText.Text = string.Empty;
            }
        }

        public void OnMouseExited()
        {
            _inventory.InfoText.Text = string.Empty;
        }

        public void SetItem(Item newItem)
        {
            if (newItem != null)
            {
                InventoryItem = newItem;
                _quantity += newItem.Quantity;
                _icon.Visible = true;
                _icon.Texture = InventoryItem.Icon;
            }
            else
            {
                InventoryItem = null;
                _icon.Texture = null;
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
            {
                _quantityLabel.Text = string.Empty;
            }
            else
            {
                _quantityLabel.Text = _quantity.ToString();
            }
        }

        public void OnPressed()
        {
            if (InventoryItem == null)
            {
                return;
            }

            var removeAfterUse = InventoryItem.OnUse(_inventory.GetParent<Player>());

            if (removeAfterUse)
            {
                InventoryItem.Free();
                RemoveItem(InventoryItem);
            }
        }
    }
}
