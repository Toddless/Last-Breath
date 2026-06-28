namespace LastBreath
{
    using System;
    using Battle.Source;
    using Core.Data;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.UI;
    using Godot;
    using LootGeneration.Source;
    using Services;
    using UI.View;
    using Utilities;

    public partial class Main : Node2D
    {
        private const string UID = "uid://cvru2meygw8jj";
        private readonly IGameServiceProvider _provider = GameServiceProvider.Instance;
        private IGameEventBus? _gameEventBus;
        [Export] private MainWorld? _mainWorld;
        [Export] private Node? _uiLayerManager;

        public override void _Ready()
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_uiLayerManager);
                ArgumentNullException.ThrowIfNull(_mainWorld);

                var manager = _provider.GetService<IUiElementsManager>();
                manager.Subscribe(_uiLayerManager);
                manager.ChangeHud(typeof(PlayerHud));
                _provider.GetService<ILootOrchestrator>().SetFloorToSpawnItems(_mainWorld);
                _gameEventBus = _provider.GetService<IGameEventBus>();
                _gameEventBus.Subscribe<BattleInitializedEvent>(OnBattleInitialized);
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to load main.", ex, this);
            }
        }

        private async void OnBattleInitialized(BattleInitializedEvent evnt)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mainWorld);
                var context = new BattleContext(evnt.Player, evnt.Entities, _mainWorld, _provider, this);
                await ToSignal(GetTree(), "process_frame");
                var result = await context.RunBattleAsync();
                context.Dispose();
                _gameEventBus?.Publish(new BattleEndEvent(result));
            }
            catch (Exception es)
            {
                GD.Print($"Exception: {es.Message}, Stack Trace: {es.StackTrace}");
                Tracker.TrackException("Failed to instantiate BattleArena", es, this);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
