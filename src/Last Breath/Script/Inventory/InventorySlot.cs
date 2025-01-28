namespace Playground.Script.Inventory
{
    using System;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Items;

    public partial class InventorySlot : Node
    {
     
        #region Private fields
        private RichTextLabel? _fullItemDescription;
        private GlobalSignals? _globalSignals;
        private Vector2 _mousePosition;
        private Label? _quantityLabel;
        private Item? _inventoryItem;
        private TextureRect? _icon;
        private Area2D? _area2D;
        private int _quantity;
        #endregion

        #region Properties
        public Item? InventoryItem
        {
            get => _inventoryItem;
            set => _inventoryItem = value;
        }

        #endregion

        public override void _Ready()
        {
           // _globalSignals = GetNode(NodePathHelper.GlobalSignalPath) as GlobalSignals;
            _fullItemDescription = GetNode<RichTextLabel>("ItemDescription");
            _quantityLabel = GetNode<Label>("QuantityText");
            // for mouseEntered and mouseExited events on each child control node mouse filter
            // should be set to ignore
            _area2D = GetNode<Area2D>(nameof(Area2D));
            _icon = GetNode<TextureRect>("Icon");
            _fullItemDescription.Hide();
            if (_quantityLabel == null)
            {
                ArgumentNullException.ThrowIfNull(_quantityLabel);
            }
        }

        public void OnMouseEntered()
        {
            // action on mouse entered.
            if (InventoryItem == null || _area2D == null)
            {
                return;
            }

            _mousePosition = _area2D.GetLocalMousePosition();
            // for example hier shown item description if under mouse cursor is an weapon
            if (InventoryItem is Weapon s)
            {
                _fullItemDescription!.Text = $"{s.ItemName} \n" +
                    $"Damage: {Mathf.RoundToInt(s.MinDamage)} - {Mathf.RoundToInt(s.MaxDamage)} \n" +
                    $"Critical Strike Chande: {s.CriticalStrikeChance * 100}% \n";
                _fullItemDescription.Show();
            }
            else if (InventoryItem is BodyArmor c)
            {
                _fullItemDescription!.Text = $"{c.ItemName}\n" +
                    $"Defence: {Mathf.RoundToInt(c.Defence)}\n" +
                    $"Bonus Health: {Mathf.RoundToInt(c.BonusHealth)}";
                _fullItemDescription.Show();
            }
        }

        public void OnMouseExited()
        {
            if (_fullItemDescription == null)
            {
                return;
            }
            _fullItemDescription?.Hide();
            _fullItemDescription!.Text = string.Empty;
            _mousePosition = Vector2.Zero;
        }

        public override void _Input(InputEvent @event)
        {
            if (_mousePosition != Vector2.Zero && @event.IsActionPressed(InputMaps.EquipOnRightClickButton) && InventoryItem != null)
            {
               // _globalSignals?.EmitSignal(GlobalSignals.SignalName.OnEquipItem, InventoryItem);
            }
        }

        public void SetItem(Item? item)
        {
            if (item != null)
            {
                InventoryItem = item;
                _quantity += item.Quantity;
                _icon!.Visible = true;
                _icon.Texture = InventoryItem.Icon;
            }
            else
            {
                InventoryItem = null;
                _icon!.Texture = null;
            }
            UpdateQuantity();
        }

        public void AddItem(Item item)
        {
            _quantity += item.Quantity;
            UpdateQuantity();
        }

        public void RemoveItem(Item? item)
        {
            if (item == null)
            {
                return;
            }

            _quantity -= item.Quantity;
            UpdateQuantity();

            if (_quantity == 0)
            {
                SetItem(null);
            }
        }

        public void RemoveItself()
        {
            if (InventoryItem == null)
            {
                return;
            }

            _quantityLabel!.Text = string.Empty;
            _quantity = 0;
            _icon!.Texture = null;
            InventoryItem = null;
        }

        public void UpdateQuantity()
        {
            if (_quantity <= 1)
            {
                _quantityLabel!.Text = string.Empty;
            }
            else
            {
                _quantityLabel!.Text = _quantity.ToString();
            }
        }
    }
}
