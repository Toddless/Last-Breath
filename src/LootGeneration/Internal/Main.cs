namespace LootGeneration.Internal
{
    using System;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Events;
    using Godot;
    using Services;
    using Source;

    internal partial class Main : Node2D
    {
        private readonly Spawner _spawner = new();
        private readonly IGameServiceProvider _gameServiceProvider = GameServiceProvider.Instance;
        [Export] private MainWorld? _mainWorld;
        [Export] LootGenerationHud? _lootGenerationHud;

        public override void _Ready()
        {
            LoadData();
            _lootGenerationHud?.SetAsDefault += _spawner.SetAsDefault;
            _lootGenerationHud?.SetRandomNpcCreation += _spawner.SetRandomCreation;
            _lootGenerationHud?.CreateSingleNpc += _spawner.CreateSingle;
            _gameServiceProvider.GetService<ILootOrchestrator>().SetFloorToSpawnItems(_mainWorld);
        }


        private async void LoadData()
        {
            try
            {
                var npcModifierProvider = _gameServiceProvider.GetService<INpcModifierProvider>();
                var gameEventBus = _gameServiceProvider.GetService<IGameEventBus>();
                // The stream the spawner rolls on is chosen here, from the container that already binds the
                // engine RNG for everything else: this scene runs inside Godot, so its spawns roll on the
                // engine generator too — the spawner's own default stays free of the engine for hosts
                // without one, where merely building a native generator kills the process.
                _spawner.SetRandomNumberGenerator(_gameServiceProvider.GetService<IRandomNumberGenerator>());
                _spawner.SetNpcModifierProvider(npcModifierProvider);
                _lootGenerationHud?.SetEventBus(gameEventBus);
                _lootGenerationHud?.SetNpcModifiers(npcModifierProvider.GetAllModifierIds());
                _spawner.SetWorld(_mainWorld);
                _spawner.SetGameEventBus(gameEventBus);
                _spawner.InitialSpawn();
            }
            catch (Exception e)
            {
                GD.Print($"error loading data: {e.Message}, {e.StackTrace}");
            }
        }
    }
}
