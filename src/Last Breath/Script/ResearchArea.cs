namespace Playground.Script
{
    using Godot;
    using Microsoft.Extensions.DependencyInjection;
    using Playground.Script.Enums;
    using Playground.Script.Items;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class ResearchArea : Area2D
    {
        #region Private fields

        private IBasedOnRarityLootTable? _lootTable;
        private GlobalRarity _areaRarity;
        #endregion

        #region Signals
        [Signal]
        public delegate void OnPlayerEnteredZoneEventHandler(ResearchArea zone);
        [Signal]
        public delegate void OnPlayerExitedZoneEventHandler(ResearchArea zone);
        [Signal]
        public delegate void PlayerTakeDamageOnAreaEnterEventHandler(float damage);
        #endregion

        #region Public properties
        public GlobalRarity AreaRarity
        {
            get => _areaRarity;
            set => _areaRarity = value;
        }
        #endregion

        public override void _Ready()
        {
            BodyEntered += OnPlayerEnter;
            BodyExited += OnPlayerExited;
            AreaRarity = GlobalRarity.Epic;
            _lootTable = DiContainer.ServiceProvider?.GetService<IBasedOnRarityLootTable>();
            GD.Print($"Scene initialized: {this.Name}");
        }

        private void OnPlayerEnter(Node node)
        {
            if (node is not CharacterBody2D)
            {
                return;
            }
            EmitSignal(SignalName.OnPlayerEnteredZone, this);
        }

        private void OnPlayerExited(Node node)
        {
            if (node is not CharacterBody2D)
            {
                return;
            }
            EmitSignal(SignalName.OnPlayerExitedZone, null);
        }

        public Item? GetRandomResearchEvent()
        {
            var item = _lootTable.GetRandomItem();
            return item;
        }
    }
}
