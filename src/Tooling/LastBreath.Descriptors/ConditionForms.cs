namespace LastBreath.Descriptors
{
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Enums;
    using Core.Modifiers.Conditions;

    // The Conditions catalog has no DTO: the game reads a record as raw json and hands it to whichever
    // factory the "type" names, so the reflector has no type to read a record off. These records are that
    // type, written down once — nothing is ever deserialized into them.
    //
    // ConditionRecord is what every reader meets: every key any type may write, each of them optional
    // because most records write most of them never. The forms below are what one type actually asks for,
    // so the inspector draws the four keys a threshold needs instead of the thirteen the catalog knows —
    // and each of them REQUIRES what its factory refuses the record without, so a freshly made entry
    // carries the keys that make it readable.
    //
    // Both meet in ConditionShape, whose two keys every record on disk begins with: an entry the tool
    // draws without them is one no line can name and no factory can pick.
    //
    // The families that read about the TARGET rather than the owner share their form with the owner-side
    // one: "under a third of his health" is written the same way whichever fighter it is asked about, and
    // two forms for one shape would be two places to keep the answer in.

    /// <summary>What every condition begins with, on the record and on each form alike: what it is called,
    /// and which predicate it builds.</summary>
    public abstract record ConditionShape
    {
        /// <summary>What a line names the entry by. The whole record is written once and read by everyone
        /// who names it, so the id is the entry's only address — and an entry without one is skipped.</summary>
        public required string Id { get; init; }

        /// <summary>Which predicate the entry builds — the value that picks the form. A type nobody
        /// registered a factory for is refused rather than read as "always on".</summary>
        public required string Type { get; init; }
    }

    /// <summary>One catalog entry as every reader meets it: the two keys of a condition, every key the nine
    /// types write between them, and the inversion they all share.</summary>
    public sealed record ConditionRecord : ConditionShape
    {
        /// <summary>The vital a resource predicate follows. Exactly one of the three: a combination parses
        /// cleanly and follows nothing.</summary>
        [EnumOf(typeof(Costs))] public string? Resource { get; init; }

        /// <summary>Share of that vital's maximum the line stops at.</summary>
        public float? Value { get; init; }

        /// <summary>The edge of the vital a boundary predicate sits on.</summary>
        [EnumOf(typeof(ResourceState))] public string? State { get; init; }

        /// <summary>Status names and group aliases, OR-ed into one mask.</summary>
        public string[]? Statuses { get; init; }

        /// <summary>Which of the effects a fighter carries are counted.</summary>
        [EnumOf(typeof(EffectScope))] public string? Scope { get; init; }

        /// <summary>The effect whose stacks are counted, written only by the scope that counts them.</summary>
        [CatalogRef(DataCatalog.Effects, AllowEmpty = true)] public string? EffectId { get; init; }

        /// <summary>How many the counting predicates must find; absent means one.</summary>
        public int? Count { get; init; }

        /// <summary>The stance the owner has to be in.</summary>
        [EnumOf(typeof(Stance))] public string? Stance { get; init; }

        /// <summary>Which action of the owner's a turn-scoped predicate counts.</summary>
        [EnumOf(typeof(TurnAction))] public string? Action { get; init; }

        /// <summary>The turn of the battle the line stops at, counted from one.</summary>
        public int? Turn { get; init; }

        /// <summary>Inverts the predicate. Shared by every type and applied outside the factories, and
        /// written LAST — after whatever the type asked for, which is where every record on disk has it and
        /// where the key order has to keep it.</summary>
        public bool? Negate { get; init; }
    }

    /// <summary>"While the vital sits under a share of its maximum" — asked about the owner
    /// (<see cref="ConditionTypes.ResourceThreshold"/>) or about whoever he is hitting
    /// (<see cref="ConditionTypes.TargetResourceThreshold"/>).</summary>
    public sealed record ConditionOverAResourceShare : ConditionShape
    {
        /// <summary>Health, Mana or Barrier — the three a predicate has a signal for.</summary>
        [EnumOf(typeof(Costs))] public required string Resource { get; init; }

        /// <summary>The share of the maximum, in the game's own scale: 0.3 is a third. A record writing
        /// none is refused rather than read as zero.</summary>
        public required float Value { get; init; }
    }

    /// <summary>"While the vital sits on its edge" — full, or nothing left
    /// (<see cref="ConditionTypes.ResourceState"/>). The one predicate that answers "there is no barrier
    /// at all", which the share-based one deliberately does not.</summary>
    public sealed record ConditionOverAResourceBoundary : ConditionShape
    {
        [EnumOf(typeof(Costs))] public required string Resource { get; init; }

        [EnumOf(typeof(ResourceState))] public required string State { get; init; }
    }

    /// <summary>"While one of these statuses is on him" — the owner
    /// (<see cref="ConditionTypes.Status"/>) or whoever he is hitting
    /// (<see cref="ConditionTypes.TargetStatus"/>).</summary>
    public sealed record ConditionOverStatuses : ConditionShape
    {
        /// <summary>The names are a status of the fight OR one of the groups the design talks in — the
        /// aliases of <see cref="StatusMasks"/>, which stand for several statuses at once. Two vocabularies
        /// under one key, which the markup has no way of stating: an enum offered here would call every
        /// alias the shipped file writes a stranger, so the words are left free and a test in the game
        /// suite holds them against what the mask reader resolves. A record naming none is refused.</summary>
        public required string[] Statuses { get; init; }
    }

    /// <summary>"While I carry at least N of these" (<see cref="ConditionTypes.Effect"/>): a shield
    /// holding, N stacks of one named effect, N debuffs, or — inverted — nothing at all.</summary>
    public sealed record ConditionOverCarriedEffects : ConditionShape
    {
        [EnumOf(typeof(EffectScope))] public required string Scope { get; init; }

        /// <summary>The effect counted, which only the stack-counting scope asks for — every other scope
        /// counts a family and names nothing.</summary>
        [CatalogRef(DataCatalog.Effects, AllowEmpty = true)] public string? EffectId { get; init; }

        /// <summary>How many to find; absent means one. Below one is refused — it would be met by
        /// everyone.</summary>
        public int? Count { get; init; }
    }

    /// <summary>"While I fight in this stance" (<see cref="ConditionTypes.Stance"/>).</summary>
    public sealed record ConditionOverTheStance : ConditionShape
    {
        [EnumOf(typeof(Stance))] public required string Stance { get; init; }
    }

    /// <summary>"While the turn I am in already holds N of these"
    /// (<see cref="ConditionTypes.TurnAction"/>); inverted, it is the "first ..." half of the family.</summary>
    public sealed record ConditionOverTurnActions : ConditionShape
    {
        [EnumOf(typeof(TurnAction))] public required string Action { get; init; }

        /// <summary>How many to count; absent means one.</summary>
        public int? Count { get; init; }
    }

    /// <summary>"Until the battle reaches this turn" (<see cref="ConditionTypes.BattleTurn"/>); inverted,
    /// it covers that turn and every one after.</summary>
    public sealed record ConditionOverTheBattleTurn : ConditionShape
    {
        /// <summary>Counted from one: the turn the line stops at.</summary>
        public required int Turn { get; init; }
    }
}
