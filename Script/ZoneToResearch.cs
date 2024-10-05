namespace Playground.Script
{
    using Godot;
    using Playground.Script.Items;
    using Playground.Script.Items.Factories;
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public partial class ZoneToResearch : Area2D
    {


        // кастомный сигнал
        [Signal]
        public delegate void OnPlayerEnteredZoneEventHandler(ZoneToResearch zone);
        public GlobalRarity globalRarity;
        private BasedOnRarityLootTable lootTable = new();
        private Player player;

        public override void _Ready()
        {
            // поскольку зон может быть множество, добавляю их все в общую группу
            // в теории можно будет генерить события сразу для всех зон
            // на будущее: Найти информацию является ли это хорошей практикой
            AddToGroup("ZonesToResearch");
            this.BodyEntered += OnPlayerEnter;
            this.BodyExited += OnPlayerExited;
            this.globalRarity = GlobalRarity.Epic;
            lootTable.InitializeLootTable();
            lootTable.ValidateTable();
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
            EmitSignal(nameof(OnPlayerEnteredZone), null);
        }

        public Item AddEvents()
        {
            var item = lootTable.GetRandomItem();
            return item;
        }
    }
}
