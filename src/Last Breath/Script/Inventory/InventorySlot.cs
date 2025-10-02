namespace LastBreath.Script.Inventory
{
    using Godot;
    using System;
    using Core.Enums;
    using Crafting.Source;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using Crafting.TestResources.Inventory;

    public partial class InventorySlot : Slot, IInventorySlot
    {
        private const string UID = "uid://cekpl68hghs2v";

        [Export] protected Label? QuantityLabel;

        public event Action<string, MouseButtonPressed>? OnItemClicked;

        public override void _Ready()
        {
            this.MouseEntered += OnMouseEnter;
            this.MouseExited += OnMouseExit;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            // we know we have dictionary with item here
            if (base._CanDropData(atPosition, data))
            {
                var source = GetNodeOrNull<Slot>(data.AsGodotDictionary()["Source"].AsNodePath());
                if (source == null) return false;
                bool isCraftingItem = CurrentItem != null && (ItemDataProvider.Instance?.IsItemImplement<IResource>(CurrentItem.ItemId) ?? true);
                if (source is CraftingSlot && isCraftingItem) return false;
            }
            return true;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        protected override void RefreshUI()
        {
            base.RefreshUI();
            if (QuantityLabel != null) QuantityLabel.Text = Quantity > 1 ? Quantity.ToString() : string.Empty;
        }

        protected override void OnMouseExit()
        {

        }

        protected override void OnMouseEnter()
        {

        }
    }
}
