namespace Crafting.Internal.Layers
{
    using Core.Views.UI;
    using Godot;
    using Inventory;
    using Services;
    using Source.UIElements;

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
                _elementsManager?.ToggleWindow(typeof(CraftingWindow));
                GetViewport().SetInputAsHandled();
            }

            if (@event is InputEventKey { Pressed: true, Keycode: Key.I })
            {
                _elementsManager?.ToggleWindow(typeof(InventoryWindow));
                GetViewport().SetInputAsHandled();
            }
        }
    }
}
