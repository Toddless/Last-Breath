namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Inventory;
    using Godot;

    public interface ILootOrchestrator
    {
        /// <summary>Drops currently lying on the floor and pickable — scavenger NPCs read this to plan.</summary>
        IReadOnlyList<ItemOnGround> ItemsOnGround { get; }

        void SetFloorToSpawnItems(Node2D? floor);

        /// <summary>
        /// Moves one drop into the given inventory. No reach check here — the caller (player click
        /// handler, goblin AI, ...) owns positioning. Returns false when the inventory refused it.
        /// </summary>
        bool TryPickup(ItemOnGround item, IInventory inventory);
    }
}
