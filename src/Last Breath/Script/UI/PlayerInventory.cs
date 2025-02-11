namespace Playground.Script.UI
{
    using Godot;
    using Playground.Components;
    using Playground.Script.Helpers;

    public partial class PlayerInventory : Control
    {
        private GridContainer? _equipInventory, _craftInventory;
        private Inventory? _inventoryEquip, _inventoryCrafting;
        public override void _Ready()
        {
            var root = GetNode<MarginContainer>(nameof(MarginContainer));
            var box = root.GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<VBoxContainer>("VBoxContainerInventory").GetNode<TabContainer>(nameof(TabContainer));
            _inventoryEquip = new Inventory();                         
            _inventoryCrafting = new Inventory();
            _craftInventory = box.GetNode<TabBar>("Crafting").GetNode<GridContainer>(nameof(GridContainer));
            _equipInventory = box.GetNode<TabBar>("Equip").GetNode<GridContainer>(nameof(GridContainer));
            _inventoryEquip.Initialize(144, ScenePath.InventorySlot, _equipInventory!);
            _inventoryCrafting.Initialize(144, ScenePath.InventorySlot, _craftInventory!);
        }
    }
}
