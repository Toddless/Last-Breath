namespace LootGeneration.Internal
{
    using Source;
    using Godot;
    using System;
    using Services;
    using Core.Data;
    using Spawner = temp.Spawner;
    using Core.Interfaces.Events;

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
