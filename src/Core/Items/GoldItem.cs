namespace Core.Items
{
    using System;
    using System.Linq;
    using Constants;
    using Enums;
    using Godot;

    /// <summary>Marker for money-like drops: a currency pile NEVER enters the bag — picking it up
    /// credits the wallet instead (owner decision 2026-07-24: gold lies on the floor like loot).</summary>
    public interface ICurrencyItem : IItem
    {
    }

    /// <summary>The gold pile: minted by the loot pipeline from the kill's leftover budget and by
    /// future reward channels. Stack = the gold amount; the instance itself carries no state.</summary>
    public class GoldItem : ICurrencyItem
    {
        public const string ItemId = "Item_Gold";

        public string Id => ItemId;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Rarity Rarity { get; set; } = Rarity.Common;
        public int MaxStackSize => int.MaxValue;
        public string[] Tags { get; } = ["Currency", "Gold"];
        public string DisplayName => Localization.Localization.Localize(Id);
        public string Description => Localization.Localization.LocalizeDescription(Id);

        // Lazy like every item icon: hosts without the Godot runtime must never touch it.
        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                string path = AssetPaths.ResourceIcon(ItemId);
                field = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
                return field;
            }
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>() => (T)(object)new GoldItem();
    }
}
