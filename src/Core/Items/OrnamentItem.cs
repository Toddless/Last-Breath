namespace Core.Items
{
    using System;
    using System.Linq;
    using Constants;
    using Enums;
    using Godot;

    /// <summary>
    /// An ornament away from the ability wearing it — the form in which one is handed over, carried and
    /// put away. There are a handful in the whole game and they are meant to MOVE between abilities, so
    /// being a thing is the whole of it: the bag is where an ornament waits between builds, and until it
    /// is an item there is no state in which the player holds one without it doing anything.
    /// <para>
    /// Unlike an augment, an ornament carries nothing rolled: what it is, is its record. That is why the
    /// bag copy can be MINTED again when the ornament comes off an ability instead of being kept
    /// somewhere for the trip — there is nothing about this one that another copy of the same record
    /// would not say.
    /// </para>
    /// </summary>
    public interface IOrnamentItem : IItem
    {
        /// <summary>Which tier of socket attaching it grants. Read off the item because whoever attaches
        /// one holds the item and not the catalog — the tier travels with the thing.</summary>
        int Tier { get; }
    }

    /// <inheritdoc cref="IOrnamentItem"/>
    /// <remarks>Born through <see cref="IOrnamentMinter"/> and nowhere else: an ornament with no record
    /// behind it would grant a socket the catalog never declared.</remarks>
    public sealed class OrnamentItem(string id, int tier, Rarity rarity) : IOrnamentItem
    {
        /// <summary>The one word the bag knows every ornament by. What tier it grants is the record's to
        /// say and is read there; carrying it as a tag as well would leave the tier written twice.</summary>
        public const string ItemTag = "Ornament";

        public string Id { get; } = id;

        public int Tier { get; } = tier;

        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Rarity Rarity { get; set; } = rarity;

        /// <summary>Never more than one to a slot: an ornament is attached one at a time and a pile of
        /// them could not say which one left it.</summary>
        public int MaxStackSize => 1;

        public string[] Tags { get; } = [ItemTag];

        public string DisplayName => Localization.Localization.Localize(Id);

        public string Description => Localization.Localization.Localize($"{Id}_Description");

        // Lazy like every item icon: hosts without the Godot runtime must never touch it.
        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                string path = AssetPaths.ItemIcon(Id);
                field = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
                return field;
            }
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public T Copy<T>() => (T)(object)new OrnamentItem(Id, Tier, Rarity);
    }
}
