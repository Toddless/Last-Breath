namespace LastBreath.Script.ScenesHandlers
{
    using Godot;
    using System;
    using Core.Data;
    using Utilities;
    using LastBreath.Script.UI;
    using LastBreath.DIComponents;

    public partial class Main : Node2D
    {
        private const string UID = "uid://drgs10sgp405d";

        [Export] private MainWorld? _mainWorld;
        [Export] private Node? _uiLayerManager;

        public override void _Ready()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_uiLayerManager);
                ArgumentNullException.ThrowIfNull(_mainWorld);
                var serviceProvider = GameServiceProvider.Instance;
                serviceProvider.GetService<IUiElementProvider>().Subscribe(_uiLayerManager);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to load main.", ex, this);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
