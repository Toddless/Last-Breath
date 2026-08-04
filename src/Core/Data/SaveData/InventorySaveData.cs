namespace Core.Data.SaveData
{
    using System.Collections.Generic;

    /// <summary>The bag contents: a flat list of stacks. Equip items carry their full rolled state
    /// (<see cref="EquipItemSaveData"/>), augments the numbers their copy rolled
    /// (<see cref="AugmentSaveData"/>); stackable resources are just an id and an amount.</summary>
    public class InventorySaveData
    {
        public List<InventoryItemSaveData> Items { get; set; } = [];
    }

    public class InventoryItemSaveData
    {
        public int Amount { get; set; }

        /// <summary>Set for template/stackable items — restored fresh from the item data by id.</summary>
        public string? ResourceId { get; set; }

        /// <summary>Set for procedurally rolled equip items — restored with their exact modifiers.</summary>
        public EquipItemSaveData? Equip { get; set; }

        /// <summary>Set for an augment copy waiting in the bag — restored with the numbers it rolled.
        /// The same shape a seated augment is written in: a bag and a socket are two places for one
        /// thing, and one of them holding a private format would make the trip between them a
        /// translation.</summary>
        public AugmentSaveData? Augment { get; set; }
    }
}
