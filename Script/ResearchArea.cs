namespace Playground.Script
{
    using Godot;
    using Playground.Script.Items;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class ResearchArea : Area2D
    {
        #region Private fields
        private BasedOnRarityLootTable _lootTable = new();
        #endregion

        #region Signals
        [Signal]
        public delegate void OnPlayerEnteredZoneEventHandler(ResearchArea zone);
        [Signal]
        public delegate void OnPlayerExitedZoneEventHandler(ResearchArea zone);
        [Signal]
        public delegate void PlayerTakeDamageOnAreaEnterEventHandler(float damage);
        #endregion

        private GlobalRarity _areaRarity;

        #region Public properties
        public GlobalRarity AreaRarity
        {
            get => _areaRarity;
            set => _areaRarity = value;
        }
        #endregion

        public override void _Ready()
        {
            // поскольку зон может быть множество, добавляю их все в общую группу
            // в теории можно будет генерить события сразу для всех зон
            // на будущее: Найти информацию является ли это хорошей практикой
            BodyEntered += OnPlayerEnter;
            BodyExited += OnPlayerExited;
            AreaRarity = GlobalRarity.Epic;
            _lootTable.InitializeLootTable();
            _lootTable.ValidateTable();
        }

        private void OnPlayerEnter(Node node)
        {
            if (node is not CharacterBody2D)
            {
                return;
            }
            EmitSignal(SignalName.PlayerTakeDamageOnAreaEnter, 250f);
            // когда игрок пересекает зону, отправляем сигнал
            EmitSignal(SignalName.OnPlayerEnteredZone, this);
        }

        private void OnPlayerExited(Node node)
        {
            if (node is not CharacterBody2D)
            {
                return;
            }
            // при покидании зоны игроком обнуляем данный сигнал
            EmitSignal(SignalName.OnPlayerExitedZone, null);
        }

        public Item GetRandomResearchEvent()
        {
            var item = _lootTable.GetRandomItem();
            return item;
        }
    }
}
