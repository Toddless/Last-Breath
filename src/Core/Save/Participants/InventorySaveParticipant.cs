namespace Core.Save.Participants
{
    using Battle.Abilities;
    using Data;
    using Data.SaveData;
    using Inventory;
    using Items;
    using Newtonsoft.Json.Linq;

    /// <summary>Persists the bag. Rolled equip items round-trip through <see cref="EquipItemSaveConverter"/>
    /// and augment copies through <see cref="IAugmentItemMinter"/> (both unique, non-reproducible numbers);
    /// stackable resources are stored as id + amount and rebuilt from item data.</summary>
    /// <param name="augments">Optional: a composition that can't build augments holds none. A file that
    /// does carry one is reported rather than dropped silently — the copy's numbers can't be rolled again.</param>
    public class InventorySaveParticipant(
        IInventory inventory,
        IItemDataProvider itemData,
        EquipItemSaveConverter converter,
        IAugmentItemMinter? augments = null) : ISaveParticipant
    {
        public string SectionId => "inventory";
        public int Version => 6; // v5: augment copies in the bag; v6: their rarity is written by name
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
                    Values = new(augment.Augment.Values),
                    Rarity = augment.Augment.Rarity,
                    Effect = augment.Augment.EffectId
                }
            },
            _ => new() { Amount = amount, ResourceId = item.Id },
        };

        /// <summary>An unrecognized record, or a build that mints no augments, can't reproduce the
        /// copy — the loss is reported rather than silent.</summary>
        private void RestoreAugment(AugmentSaveData saved)
        {
            if (augments?.Remembered(saved.Augment, saved.Values, saved.Rarity, saved.Effect) is { } copy && augments.Restore(copy) is { } item)
                inventory.TryAddItem(item);
            else
                Tracker.TrackNotFound($"Augment record '{saved.Augment}' held in the bag", this);
        }
    }
}
