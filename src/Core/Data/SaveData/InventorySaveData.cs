namespace Core.Data.SaveData
{
    using System.Collections.Generic;

    /// <summary>The bag contents: a flat list of stacks. Equip items carry their full rolled state
    /// (<see cref="EquipItemSaveData"/>); stackable resources are just an id and an amount.</summary>
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
    }
}
