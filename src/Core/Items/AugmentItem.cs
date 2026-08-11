namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Battle.Abilities;
    using Constants;
    using Enums;
    using Godot;

    /// <summary>
    /// An augment away from the socket it is meant for — the form in which one is found, carried,
    /// handed over and put away. What makes it an item at all is that it can be owned: the bag, the
    /// save file and the trader speak of things, and until an augment is one of them a player has no
    /// way to come by a single augment in the game.
    /// <para>
    /// The copy it wraps is the whole of it (<see cref="AugmentInstance"/>): the record says what the
    /// augment is about, the copy says what THIS one rolled, and the item is only the copy's passage
    /// through the world. Read through this interface rather than off the concrete class, the way the
    /// bag reads equipment and a money pile — whoever takes the augment out of the bag and seats it
    /// needs the copy, not the wrapper.
    /// </para>
    /// </summary>
    public interface IAugmentItem : IItem
    {
        /// <summary>The copy carried, with the numbers it was minted at.</summary>
        AugmentInstance Augment { get; }
    }

    /// <inheritdoc cref="IAugmentItem"/>
    /// <remarks>
    /// Born through <see cref="IAugmentItemMinter"/> and nowhere else: the copy inside has to come
    /// from a draw, and a second door to one would be a second set of roll rules.
    /// </remarks>
    public sealed class AugmentItem : IAugmentItem
    {
        /// <summary>The one word the bag knows every augment by, whatever the augment is about. The
        /// record's own tags describe what it improves and are read off the record where that
        /// question is asked; carrying them here as well would leave an augment's tags written in two
        /// places, free to disagree.</summary>
        public const string ItemTag = "Augment";

        /// <param name="augment">The copy this item is the passage of. Its rarity is the copy's own —
        /// drawn at the mint from the record's band and written down with its numbers. The consequence
        /// is deliberate: rebalancing a record moves neither the numbers nor the rarity of the copies
        /// already in the world.</param>
        public AugmentItem(AugmentInstance augment)
        {
            Augment = augment;
            Rarity = augment.Rarity;
        }

        public AugmentInstance Augment { get; }

        /// <summary>The record's id: what every copy of the augment shares. Two copies of one record
        /// answer the same here and differ by <see cref="InstanceId"/>, which is the whole reason an
        /// augment never stacks.</summary>
        public string Id => Augment.AugmentId;

        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Rarity Rarity { get; set; }

        /// <summary>Never more than one to a slot: two copies of a record carry different numbers, so
        /// a pile of them could not say what is in it.</summary>
        public int MaxStackSize => 1;

        public string[] Tags { get; } = [ItemTag];

        public string DisplayName => Localization.Localization.Localize(Id);

        /// <summary>What this copy does, in its own numbers rather than the record's bases — the same
        /// template the seated augment prints, filled from the same dictionary, so the augment reads
        /// the same in the bag as it will in the slot.</summary>
        public string Description => Localization.Localization.RenderDescription(Id, Printed());

        // Lazy like every item icon: hosts without the Godot runtime must never touch it. Augment art
        // lives with the abilities' — an augment is named after what it improves — and a record with
        // no art yet simply shows none instead of failing the load.
        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                string path = AssetPaths.AbilityIcon(Id);
                field = ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
                return field;
            }
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        /// <summary>Another passage for the same copy: the numbers are shared because they are frozen,
        /// the identity is not — a copy handed on is still a separate thing in the bag.</summary>
        public T Copy<T>() => (T)(object)new AugmentItem(Augment);

        private Dictionary<string, object?> Printed() =>
            Augment.Values.ToDictionary(rolled => rolled.Key, rolled => (object?)rolled.Value);
    }
}
