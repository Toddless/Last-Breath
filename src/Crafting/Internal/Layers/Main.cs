namespace Crafting.Internal.Layers
{
    using Core.Interfaces.UI;
    using Services;
    using Source.UIElements;
    using Godot;
    using Inventory;

    public partial class Main : Node2D
    {
        [Export] private UILayerManager? _uiLayerManager;
        private IUiElementsManager? _elementsManager;

        public override void _Ready()
        {
            _elementsManager = GameServiceProvider.Instance.GetService<IUiElementsManager>();
            if (_uiLayerManager != null) _elementsManager.Subscribe(_uiLayerManager);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey { Pressed: true, Keycode: Key.C })
            {
                _elementsManager?.OpenWindow(typeof(CraftingWindow));
                GetViewport().SetInputAsHandled();
            }

            if (@event is InputEventKey { Pressed: true, Keycode: Key.I })
            {
                _elementsManager?.OpenWindow(typeof(InventoryWindow));
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
