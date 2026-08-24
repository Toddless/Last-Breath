namespace Core.Views
{
    using System.Collections.Generic;
    using Battle.Abilities;
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
    /// <param name="AugmentTier">The tier the AUGMENT is written at — what its card says, and what it
    /// said in the bag. Never the slot's: a tier-one copy dropped into a tier-three socket is still a
    /// tier-one copy, and a card borrowing the slot's number would make the same augment read one way in
    /// the bag and another in the socket. Zero while the cell is empty, and zero for a record the catalog
    /// no longer declares.</param>
    /// <param name="AugmentId">The record in the cell; empty while the cell is.</param>
    /// <param name="DisplayName">The copy's name; empty while the cell is.</param>
    /// <param name="Description">What THIS copy does, in the numbers it rolled.</param>
    /// <param name="Icon">The augment's art; null when the record has none yet.</param>
    /// <param name="Rarity">Where the record stands on the item scale — the cell's frame colour.</param>
    /// <param name="Activity">How much of it is running. Meaningless unless <see cref="Kind"/> is
    /// <see cref="AugmentCellKind.Filled"/>.</param>
    /// <param name="Tags">What the seated RECORD is about — the tags an ability has to share with it, so
    /// the copy says where it belongs in the socket exactly as it said it in the bag. Empty while the
    /// cell is, and empty for a record the catalog no longer declares.</param>
    /// <param name="AbilityId">The one ability the record was written for; empty when it was written for
    /// none.</param>
    /// <param name="FitsAnyAbility">Whether the record claims every ability there is.</param>
    public record AugmentCellView(
        string SocketAddress,
        AugmentCellKind Kind,
        int Tier,
        int AugmentTier,
        string AugmentId,
        string DisplayName,
        string Description,
        Texture2D? Icon,
        Rarity Rarity,
        AugmentActivity Activity,
        IReadOnlyList<string> Tags,
        string AbilityId,
        bool FitsAnyAbility);
}
