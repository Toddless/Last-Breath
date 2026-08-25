namespace Core.Save
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.SaveData;
    using Items;

    /// <summary>Round-trips one owned item through <see cref="InventoryItemSaveData"/> — the single shape
    /// every place that writes an item down uses: a rolled equip with its exact lines, an augment copy with
    /// the numbers it drew, anything else as an id rebuilt from item data. A bag stack and a shelf slot are
    /// two places for one thing, and a private format in either would make the trip between them a
    /// translation.</summary>
    /// <param name="augments">Optional: a composition that mints no augments rebuilds none, and an entry
    /// naming one is answered with null rather than with a different augment.</param>
    public class InventoryItemSaveConverter(
        EquipItemSaveConverter equips,
        IItemDataProvider itemData,
        IAugmentItemMinter? augments = null)
    {
        /// <summary>What the entry has to say about the item, by the kind of thing it is: everything
        /// rolled is written out, everything template is written as its id.</summary>
        public InventoryItemSaveData ToData(IItem item, int amount) => item switch
        {
            IEquipItem equip => new() { Amount = amount, Equip = equips.ToData(equip) },
            IAugmentItem augment => new()
            {
                Amount = amount,
                Augment = new AugmentSaveData
                {
                    Augment = augment.Augment.AugmentId,
                    Values = new Dictionary<string, float>(augment.Augment.Values),
                    Rarity = augment.Augment.Rarity,
                    Effect = augment.Augment.EffectId
                }
            },
            _ => new() { Amount = amount, ResourceId = item.Id }
        };

        /// <summary>The item the entry names, or null when the copy's numbers can no longer be reproduced —
        /// an augment record the catalog dropped, or a build that mints none. The count travels beside the
        /// item rather than inside it: how many units an entry is worth is the caller's own question.</summary>
        public IItem? FromData(InventoryItemSaveData data)
        {
            if (data.Equip != null) return equips.FromData(data.Equip);
            if (data.Augment != null) return FromAugmentData(data.Augment);
            return string.IsNullOrEmpty(data.ResourceId) ? null : Template(data.ResourceId);
        }

        private IItem? FromAugmentData(AugmentSaveData saved) =>
            augments?.Remembered(saved.Augment, saved.Values, saved.Rarity, saved.Effect) is { } copy
                ? augments.Restore(copy)
                : null;

        /// <summary>An id the game data no longer holds costs its own entry and nothing more: the store
        /// answers such an id by throwing, and letting that out would cost the whole section — a single
        /// retired resource would empty a bag or a shelf.</summary>
        private IItem? Template(string resourceId)
        {
            try
            {
                return itemData.CopyItem(resourceId);
            }
            catch (Exception)
            {
                Tracker.TrackNotFound($"Item '{resourceId}' a save file holds", this);
                return null;
            }
        }
    }
}
