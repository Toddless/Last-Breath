namespace Core.Items
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Enums;

    public interface IItemMinter
    {
        /// <summary>Type-agnostic birth point for callers that only hold an id (quests, narrative,
        /// debug): the kind of thing the id names decides how it is born.</summary>
        /// <param name="rarity">What the thing is worth, when the caller already knows — a loot table
        /// seat that names a set says what the set is worth. Null leaves every kind to its own draw,
        /// which is what an id on its own says about rarity: nothing.</param>
        IItem MintItem(string id, Rarity? rarity = null);
    }

    /// <summary>
    /// The one door an id goes through when nobody holding it knows what kind of thing it names.
    /// Behind it stands a list of kinds rather than a choice between two: each is offered the id and
    /// answers with an item or with nothing, the first answer wins, and an id no kind claims is a
    /// plain template copied from the item data. A kind added to the game is a line in that list —
    /// the point of a list at all, since the id space is open and the first two kinds were not the
    /// last.
    /// </summary>
    /// <param name="augments">Optional: a composition that does not build augments (a sandbox with
    /// no combat rules to draw their numbers around) holds no such kind, and augment ids fall
    /// through to the plain copy like any other id it cannot make sense of.</param>
    public sealed class ItemMinter(
        IEquipBlueprintProvider blueprints,
        IEquipItemMinter equipMinter,
        IItemDataProvider items,
        IAugmentItemMinter? augments = null) : IItemMinter
    {
        /// <summary>The kinds in the order they are offered the id. Each returns null for an id that
        /// is not its own — asking is how the kind is chosen, so a "no" is an answer and never a
        /// report.</summary>
        private readonly IReadOnlyList<Func<string, Rarity?, IItem?>> _kinds =
        [
            // Equipment takes no rarity here: a piece is stamped with one after the mint, by whoever
            // rolls its affix lines — the two are one decision and cannot be made in two places.
            (id, _) => blueprints.GetBlueprint(id) != null ? equipMinter.Mint(id) : null,
            (id, rarity) => augments?.Mint(id, rarity),
        ];

        // Walked lazily: the kind that claims the id is the last one asked, so an id one kind owns
        // reaches neither the kinds after it nor the fallback copy.
        public IItem MintItem(string id, Rarity? rarity = null) =>
            _kinds.Select(kind => kind(id, rarity)).FirstOrDefault(item => item != null) ?? items.CopyItem(id);
    }
}
