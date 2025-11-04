namespace Battle
{
    using Godot;
    using System;
    using Utilities;
    using Battle.Services;
    using Core.Interfaces.Data;

    internal partial class Main : Node2D
    {
        private const string UID = "uid://dj86l1gr2ffle";
        [Export] private MainWorld? _mainWorld;
        [Export] private Node? _uiLayerManager;

        public override void _Ready()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_uiLayerManager);
                ArgumentNullException.ThrowIfNull(_mainWorld);
                var serviceProvider = GameServiceProvider.Instance;
                serviceProvider.GetService<IUIElementProvider>().Subscribe(_uiLayerManager);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to load main.", ex, this);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
