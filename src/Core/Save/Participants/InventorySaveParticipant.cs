namespace Core.Save.Participants
{
    using Battle.Abilities;
    using Data;
    using Data.SaveData;
    using Inventory;
    using Items;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Persists the bag. Rolled equip items round-trip through <see cref="EquipItemSaveConverter"/>
    /// (their modifiers are unique) and augment copies through <see cref="IAugmentItemMinter"/> (their
    /// numbers are); stackable resources are stored as id + amount and rebuilt from the item data.
    /// Runs at <see cref="RestoreOrder.Items"/> — the item data providers are loaded and the bag has
    /// been emptied by the session reset before this restores into it.
    /// </summary>
    /// <param name="augments">Optional: a composition that cannot build augments holds none in its
    /// bag either. A file that does carry one is reported rather than dropped in silence — the copy's
    /// numbers exist nowhere else and nothing can roll them again.</param>
    public class InventorySaveParticipant(
        IInventory inventory,
        IItemDataProvider itemData,
        EquipItemSaveConverter converter,
        IAugmentItemMinter? augments = null) : ISaveParticipant
    {
        public string SectionId => "inventory";
        public int Version => 5; // v5: augment copies in the bag
        public int RestoreOrder => Save.RestoreOrder.Items;

        public JToken Capture()
        {
            var data = new InventorySaveData();
            foreach ((IItem item, int amount) in inventory.GetContents())
                data.Items.Add(Written(item, amount));

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
                else if (entry.Augment != null)
                    RestoreAugment(entry.Augment);
                else if (!string.IsNullOrEmpty(entry.ResourceId))
                    inventory.TryAddItem(itemData.CopyItem(entry.ResourceId), entry.Amount);
            }
        }

        /// <summary>What the entry has to say about the item, by the kind of thing it is: everything
        /// rolled is written out, everything template is written as its id.</summary>
        private InventoryItemSaveData Written(IItem item, int amount) => item switch
        {
            IEquipItem equip => new() { Amount = amount, Equip = converter.ToData(equip) },
            IAugmentItem augment => new()
            {
                Amount = amount,
                Augment = new AugmentSaveData
                {
                    Augment = augment.Augment.AugmentId,
                    Values = new(augment.Augment.Values)
                }
            },
            _ => new() { Amount = amount, ResourceId = item.Id },
        };

        /// <summary>Puts the copy back exactly as it was written down. A record the catalog no longer
        /// declares — or a build that mints no augments at all — cannot produce it, and the loss is
        /// said out loud: the numbers were the copy's own and nothing can draw them again.</summary>
        private void RestoreAugment(AugmentSaveData saved)
        {
            if (augments?.Restore(new AugmentInstance(saved.Augment, saved.Values)) is { } item)
                inventory.TryAddItem(item);
            else
                Tracker.TrackNotFound($"Augment record '{saved.Augment}' held in the bag", this);
        }
    }
}
