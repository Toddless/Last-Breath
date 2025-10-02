namespace LastBreath.Script.UI.View
{
    using System;
    using Godot;
    using LastBreath.Script.Inventory;
    using LastBreath.Script.Items;

    public partial class InventoryUI : VBoxContainer
    {
        private Button? _close, _takeAll;
        private GridContainer? _grid;
        private readonly Inventory _inventory = new();

        public event Action? Close, TakeAll;

        public override void _Ready()
        {
            _close = GetNode<Button>("Close");
            _takeAll = GetNode<Button>("TakeAll");
            _grid = GetNode<GridContainer>(nameof(GridContainer));
            _inventory.Initialize(25, _grid);
            SetEvents();
        }

        public void Clear()
        {
            _inventory.Clear();
        }

        public void AddItem(Item item) => _inventory.TryAddItem(item);

        private void SetEvents()
        {
            _close.Pressed += () => Close?.Invoke();
            _takeAll.Pressed += () => TakeAll?.Invoke();
        }
    }
}
