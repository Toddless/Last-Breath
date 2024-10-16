namespace Playground
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;
    using Playground.Script.Items;

    public partial class InventorySlot : Node
    {
        #region Const
        private const string InventoryComponent = "/root/MainScene/CharacterBody2D/InventoryComponent";
        #endregion

        #region Private fields
        private RichTextLabel _fullItemDescription;
        private InventoryComponent _inventory;
        private GlobalSignals _globalSignals;
        private Vector2 _mousePosition;
        private Label _quantityLabel;
        private Item _inventoryItem;
        private TextureRect _icon;
        private Area2D _area2D;
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
            _globalSignals = GetNode(NodePathHelper.GlobalSignalPath) as GlobalSignals;
            _fullItemDescription = GetNode<RichTextLabel>("ItemDescription");
            _inventory = GetNode<InventoryComponent>(InventoryComponent);
            _quantityLabel = GetNode<Label>("QuantityText");
            // for mouseEntered and mouseExited events on each child control node mouse filter
            // should be set to ignore
            _area2D = GetNode<Area2D>(nameof(Area2D));
            _icon = GetNode<TextureRect>("Icon");
            _fullItemDescription.Hide();
        }

        public void OnMouseEntered()
        {
            // action on mouse entered.
            if(InventoryItem == null)
            {
                return;
            }

            _mousePosition = _area2D.GetLocalMousePosition();
            // for example hier im show item description if under mouse cursor is an weapon
            if (InventoryItem is Weapon s)
            {
                _fullItemDescription.Text = $" {s.ItemName} \n" +
                    $" Damage: {Mathf.RoundToInt(s.MinDamage)} - {Mathf.RoundToInt(s.MaxDamage)} \n" +
                    $" Critical Strike Chande: {s.CriticalStrikeChance * 100}% \n";
                _fullItemDescription.Show();
            }
        }

        public void OnMouseExited()
        {
            _fullItemDescription?.Hide();
            _fullItemDescription.Text = string.Empty;
            _mousePosition = Vector2.Zero;
        }

        public override void _Input(InputEvent @event)
        {
            if (_mousePosition != Vector2.Zero && @event.IsActionPressed(InputMaps.EquipOnRightClickButton))
            {
                _globalSignals.EmitSignal(GlobalSignals.SignalName.OnEquipItem, InventoryItem);
            }
        }

        public void SetItem(Item item)
        {
            if (item != null)
            {
                InventoryItem = item;
                _quantity += item.Quantity;
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
    }
}
