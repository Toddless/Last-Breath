namespace LastBreath.Descriptors
{
    using Core.Data.GameData;
    using Core.Data.Schema;

    // The three ways an augment record answers the one question that decides which sockets take it:
    // which ability it belongs on. The game reads all three into AbilityAugmentData, so nothing is ever
    // deserialized into these — they exist to say which key an author is meant to write, and which two he
    // is not. The rule reading them is Core.Battle.Abilities.AugmentFit and it refuses a record answering
    // the question twice, which is exactly what one shape at a time buys.
    //
    // Only the binding key belongs to a shape. Everything else a record carries — its tier, its rarity
    // band, the values it rolls, the effect it lays — is asked of every augment whichever way it binds,
    // and stays on the record itself.

    /// <summary>An augment written for ONE ability, which its record names. The exception kept for
    /// augments too strong to be handed to a whole family: the name decides alone, and the tags such a
    /// record happens to carry decide nothing.</summary>
    public sealed record AugmentBoundToAbility
    {
        /// <summary>The ability the augment belongs on. Its presence is what makes the record this shape
        /// rather than one of the other two, so it names an ability or the record binds nothing.</summary>
        [CatalogRef(DataCatalog.Abilities)]
        public string AbilityId { get; init; } = string.Empty;
    }

    /// <summary>An augment at home on every ability there is — what a record working through the contract
    /// every ability honours (cost, cooldown) declares. The widest reach in the system, so it is CLAIMED:
    /// a record that merely names no tag is refused rather than made universal by silence.</summary>
    public sealed record AugmentForAnyAbility
    {
        /// <summary>The claim itself. Its presence is what makes the record this shape.</summary>
        public bool FitsAnyAbility { get; init; }
    }

    /// <summary>An augment that names no ability and claims no book, and reaches an ability through one
    /// shared tag and through nothing else — the ordinary way an augment finds its family.</summary>
    public sealed record AugmentBoundByTags
    {
        /// <summary>What the augment is about. One tag shared with the ability — its own or one an
        /// installed augment granted it — is enough.</summary>
        public string[] Tags { get; init; } = [];
    }
}
