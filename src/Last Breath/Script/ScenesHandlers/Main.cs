namespace LastBreath.Script.ScenesHandlers
{
    using Godot;
    using LastBreath.Script.UI;
    using Core.Interfaces.Data;
    using LastBreath.DIComponents;

    public partial class Main : Node2D
    {
        private const string UID = "uid://drgs10sgp405d";

        [Export] private MainWorld? _mainWorld;
        [Export] private Node? _uiLayerManager;

        public override void _Ready()
        {
            if (_uiLayerManager != null) GameServiceProvider.Instance.GetService<IUIElementProvider>().Subscribe(_uiLayerManager);
            GameServiceProvider.Instance.GetService<IUIElementProvider>().CreateAndShowMainElement<PlayerHUD>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
