namespace Core.Save.Participants
{
    using Data;
    using Data.SaveData;
    using Inventory;
    using Items;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Persists the bag. Rolled equip items round-trip through <see cref="EquipItemSaveConverter"/>
    /// (their modifiers are unique); stackable resources are stored as id + amount and rebuilt from
    /// the item data. Runs at <see cref="RestoreOrder.Items"/> — the item data providers are loaded
    /// and the bag has been emptied by the session reset before this restores into it.
    /// </summary>
    public class InventorySaveParticipant(IInventory inventory, IItemDataProvider itemData, EquipItemSaveConverter converter) : ISaveParticipant
    {
        public string SectionId => "inventory";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Items;

        public JToken Capture()
        {
            var data = new InventorySaveData();
            foreach (var (item, amount) in inventory.GetContents())
                data.Items.Add(item is IEquipItem equip
                    ? new InventoryItemSaveData { Amount = amount, Equip = converter.ToData(equip) }
                    : new InventoryItemSaveData { Amount = amount, ResourceId = item.Id });

            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<InventorySaveData>();
            if (saved == null) return;

            inventory.Clear();
            foreach (var entry in saved.Items)
            {
                if (entry.Equip != null)
                    inventory.TryAddItem(converter.FromData(entry.Equip));
                else if (!string.IsNullOrEmpty(entry.ResourceId))
                    inventory.TryAddItem(itemData.CopyItem(entry.ResourceId), entry.Amount);
            }
        }
    }
}
