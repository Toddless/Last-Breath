namespace LootGeneration.Source
{
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Data.SaveData;
    using Core.Items;
    using Core.Save;
    using Newtonsoft.Json.Linq;

    /// <summary>Persists the drops still lying on the floor: what they are, in the shared item shape, and
    /// where they lie. A rolled piece comes back the one that fell — the numbers are written down, never
    /// drawn again.</summary>
    /// <param name="augments">Optional, as everywhere an augment is written down: a build that mints none
    /// rebuilds none, and an entry naming one is dropped with a report instead of becoming another
    /// augment.</param>
    public class GroundItemsSaveParticipant(
        IGroundItemStore ground,
        IItemDataProvider itemData,
        EquipItemSaveConverter converter,
        IAugmentItemMinter? augments = null) : ISaveParticipant
    {
        private readonly InventoryItemSaveConverter _items = new(converter, itemData, augments);

        public string SectionId => "groundItems";
        public int Version => 1;
        public int RestoreOrder => Core.Save.RestoreOrder.GroundItems;

        public JToken Capture() => JToken.FromObject(new GroundItemsSaveData
        {
            Items = [.. ground.CaptureGroundItems().Select(Written)]
        });

        /// <summary>The whole floor is built before any of it is laid out, so the world changes once.</summary>
        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<GroundItemsSaveData>();
            if (saved == null) return;
            ground.RestoreGroundItems([.. saved.Items.Select(Read).OfType<GroundItemPlacement>()]);
        }

        /// <summary>The floor is scene state, not a session singleton: without this a file that says
        /// nothing about drops would be loaded onto the loot of the playthrough being left behind.</summary>
        public void RestoreWithoutSection() => ground.RestoreGroundItems([]);

        private GroundItemSaveData Written(GroundItemPlacement placement) => new()
        {
            Item = _items.ToData(placement.Item, placement.Quantity),
            X = placement.X,
            Y = placement.Y
        };

        /// <summary>A drop whose copy can no longer be reproduced costs its own spot, not the floor.</summary>
        private GroundItemPlacement? Read(GroundItemSaveData saved)
        {
            if (_items.FromData(saved.Item) is not { } item)
            {
                Tracker.TrackNotFound($"A drop a save file leaves lying at ({saved.X}, {saved.Y})", this);
                return null;
            }

            return new GroundItemPlacement(item, saved.Item.Amount, saved.X, saved.Y);
        }
    }
}
