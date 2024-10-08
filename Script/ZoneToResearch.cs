namespace Playground.Script
{
    using Godot;
    using Playground.Script.Items;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class ZoneToResearch : Area2D
    {
        private BasedOnRarityLootTable _lootTable = new();
        // кастомный сигнал для входа игрока в зону
        [Signal]
        public delegate void OnPlayerEnteredZoneEventHandler(ZoneToResearch zone);
        // кастомный сигнал для выхода игрока из зоны
        [Signal]
        public delegate void OnPlayerExitedZoneEventHandler(ZoneToResearch zone);
        public GlobalRarity globalRarity;

        public override void _Ready()
        {
            // поскольку зон может быть множество, добавляю их все в общую группу
            // в теории можно будет генерить события сразу для всех зон
            // на будущее: Найти информацию является ли это хорошей практикой
            BodyEntered += OnPlayerEnter;
            BodyExited += OnPlayerExited;
            globalRarity = GlobalRarity.Epic;
            _lootTable.InitializeLootTable();
            _lootTable.ValidateTable();
        }

        private void OnPlayerEnter(Node node)
        {
            if (node is not CharacterBody2D)
            {
                return;
            }
            // когда игрок пересекает зону, отправляем сигнал
            EmitSignal(nameof(OnPlayerEnteredZone), this);
        }

        private void OnPlayerExited(Node node)
        {
            if (node is not CharacterBody2D)
            {
                return;
            }
            // при покидании зоны игроком обнуляем данный сигнал
            EmitSignal(nameof(OnPlayerExitedZone), null);
        }

        public Item ResearchArea()
        {
            var item = _lootTable.GetRandomItem();
            return item;
        }
    }
}
