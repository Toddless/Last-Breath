namespace Playground
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Inventory;

    public partial class EnemyAI : Node2D
    {
        private string _inventorySlotPath = SceneParh.InventorySlot;
        private InventoryComponent? _inventoryComponent;
        private GridContainer? _inventoryContainer;
        private HealthComponent? _healthComponent;
        private AttackComponent? _attackComponent;
        private List<InventorySlot> _slots = [];
        private PackedScene? _inventorySlot;
        private Panel? _inventoryWindow;
        private int _size = 25;
        private Area2D? _area;


        public override void _Ready()
        {
            _inventorySlot = ResourceLoader.Load<PackedScene>(_inventorySlotPath);
            _inventoryComponent = GetNode<InventoryComponent>("/root/MainScene/Enemy/InventoryComponent");
            _inventoryContainer = GetNode<GridContainer>("/root/MainScene/Enemy/InventoryComponent/InventoryWindow/InventoryContainer");
            _healthComponent = GetNode<HealthComponent>("/root/MainScene/Enemy/HealthComponent");
            _attackComponent = GetNode<AttackComponent>("/root/MainScene/Enemy/AttackComponent");
            _inventoryWindow = GetNode<Panel>("/root/MainScene/Enemy/InventoryComponent/InventoryWindow");
            _area = GetNode<Area2D>("/root/MainScene/Enemy/Area2D");
            _inventoryComponent.Initialize(_size);
            _inventoryWindow.Visible = true;
            GD.Print($"Enemies health: {_healthComponent.CurrentHealth}");
        }
    }
}
