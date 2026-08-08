namespace Core.Views
{
    using Enums;
    using Godot;

    /// <summary>What a socket cell is: free, filled, or a leftover the player may only empty.</summary>
    public enum AugmentCellKind
    {
        /// <summary>A live slot with nothing in it.</summary>
        Empty,

        /// <summary>A live slot with an augment in it.</summary>
        Filled,

        /// <summary>The node behind the slot is gone and the augment in it is not: it can be taken out
        /// and nothing can be put in. There is no empty <c>Held</c> — such a slot ceases to exist the
        /// moment it is emptied.</summary>
        Held
    }

    /// <summary>How much of what a seated augment offers is actually running. Read only for a
    /// <see cref="AugmentCellKind.Filled"/> cell — an augment in a closed slot does nothing at all, and
    /// that is a different thing said by the kind.</summary>
    public enum AugmentActivity
    {
        /// <summary>Everything it moves, it moves.</summary>
        Working,

        /// <summary>Some of its moves lost to a stronger augment on the same parameter, the rest run.</summary>
        Partly,

        /// <summary>Nothing it moves gets through: every one of its moves lost.</summary>
        Dormant
    }

    /// <summary>
    /// One augment cell of a socket row — the whole of what the panel draws in it. Everything is
    /// display data: the copy's own name and numbers, not the record's averages.
    /// </summary>
    /// <param name="SocketAddress">The slot, as the board names it
    /// (<see cref="Battle.Abilities.AbilitySocketPlacement.Address"/>). Opaque: the interface carries it
    /// back into a judgement, an install or an extraction and never takes it apart or builds one.</param>
    /// <param name="Kind">Free, filled, or remove-only.</param>
    /// <param name="Tier">The tier the SLOT takes, not the one the augment is written at: the player
    /// looks at the slot, and it is the slot a refusal about tiers is about. Repeats are legal — an
    /// ornament is a second node of the same tier on one ability.</param>
    /// <param name="AugmentId">The record in the cell; empty while the cell is.</param>
    /// <param name="DisplayName">The copy's name; empty while the cell is.</param>
    /// <param name="Description">What THIS copy does, in the numbers it rolled.</param>
    /// <param name="Icon">The augment's art; null when the record has none yet.</param>
    /// <param name="Rarity">Where the record stands on the item scale — the cell's frame colour.</param>
    /// <param name="Activity">How much of it is running. Meaningless unless <see cref="Kind"/> is
    /// <see cref="AugmentCellKind.Filled"/>.</param>
    public record AugmentCellView(
        string SocketAddress,
        AugmentCellKind Kind,
        int Tier,
        string AugmentId,
        string DisplayName,
        string Description,
        Texture2D? Icon,
        Rarity Rarity,
        AugmentActivity Activity);
}
