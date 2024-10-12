namespace Playground
{
    using Godot;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class InventorySlot : Node
    {
        #region Private fields
        private Label _quantityLabel;
        private InventoryComponent _inventory;
        private Item _inventoryItem;
        private TextureRect _icon;
        private int _quantity;
        private int _index;
        #endregion

        #region Properties
        public InventoryComponent Inventory
        {
            get => _inventory;
            set => _inventory = value;
        }

        public int Quantity
        {
            get => _quantity;
            private set => _quantity = value;
        }

        public Item InventoryItem
        {
            get => _inventoryItem;
            private set => _inventoryItem = value;
        }

        #endregion

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

        public void RemoveItself()
        {
            _quantityLabel.Text = string.Empty;
            _quantity = 0;
            _icon.Texture = null;
            InventoryItem = null;
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
        }
    }
}
