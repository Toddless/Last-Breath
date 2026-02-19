namespace LootGeneration.Source
{
    using Godot;

    public interface ILootOrchestrator
    {
        void SetFloorToSpawnItems(Node2D? floor);
    }
}
