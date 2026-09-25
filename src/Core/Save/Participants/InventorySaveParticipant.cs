namespace Core.Save.Participants
{
    using Data;
    using Data.SaveData;
    using Inventory;
    using Items;
    using Newtonsoft.Json.Linq;

    /// <summary>Persists the bag. Every stack is written in the shared item shape
    /// (<see cref="InventoryItemSaveConverter"/>): rolled equipment and augment copies whole, stackable
    /// resources as id + amount.</summary>
    /// <param name="augments">Optional: a composition that can't build augments holds none. A file that
    /// does carry one is reported rather than dropped silently — the copy's numbers can't be rolled again.</param>
    public class InventorySaveParticipant(
        IInventory inventory,
        IItemDataProvider itemData,
        EquipItemSaveConverter converter,
        IAugmentItemMinter? augments = null) : ISaveParticipant
    {
        private readonly InventoryItemSaveConverter _items = new(converter, itemData, augments);

        public string SectionId => "inventory";
        public int Version => 6; // v5: augment copies in the bag; v6: their rarity is written by name
        public int RestoreOrder => Save.RestoreOrder.Items;

        public JToken Capture()
        {
            var data = new InventorySaveData();
            foreach ((IItem item, int amount) in inventory.GetContents())
                data.Items.Add(_items.ToData(item, amount));

            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<InventorySaveData>();
            if (saved == null) return;

            inventory.Clear();
            foreach (var entry in saved.Items)
                RestoreEntry(entry);
        }

        /// <summary>An unrecognized augment record, or a build that mints no augments, can't reproduce the
        /// copy — the loss is reported rather than silent.</summary>
        private void RestoreEntry(InventoryItemSaveData entry)
        {
            // A count the file failed to state still puts one thing in the bag: the bag refuses an
            // amount of none outright, and a silently eaten item is worse than a wrong count.
            if (_items.FromData(entry) is { } item)
                inventory.TryAddItem(item, entry.Amount > 0 ? entry.Amount : 1);
            else if (entry.Augment != null)
                Tracker.TrackNotFound($"Augment record '{entry.Augment.Augment}' held in the bag", this);
        }
    }
}
