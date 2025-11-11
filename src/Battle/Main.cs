namespace Battle
{
    using Godot;
    using Services;
    using Core.Interfaces.Data;

    public partial class Main : Node2D
    {
        [Export] private MainWorld? _mainWorld;
        [Export] private UiLayerManager? _layerManager;

        public override void _Ready()
        {
            var provider = GameServiceProvider.Instance.GetService<IUIElementProvider>();
            if (_layerManager != null) provider.Subscribe(_layerManager);
        }
    }
}
