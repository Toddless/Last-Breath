namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Items;

    /// <summary>
    /// The floor in terms a file can hold. Separate from <see cref="ILootOrchestrator"/> so writing the
    /// drops down never handles the scene nodes they are made of: the save section names items and spots,
    /// the orchestrator alone knows how a spot becomes a node.
    /// </summary>
    public interface IGroundItemStore
    {
        IReadOnlyList<GroundItemPlacement> CaptureGroundItems();

        /// <summary>Makes the floor hold exactly these drops: whatever lies there now goes first, so a
        /// load never mixes the restored playthrough with the one being left behind.</summary>
        void RestoreGroundItems(IReadOnlyList<GroundItemPlacement> items);
    }

    /// <summary>One drop: the thing, how many of it, and where it lies on the floor.</summary>
    public record GroundItemPlacement(IItem Item, int Quantity, float X, float Y);
}
