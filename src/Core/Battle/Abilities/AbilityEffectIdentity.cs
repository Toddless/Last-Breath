namespace Core.Battle.Abilities
{
    /// <summary>
    /// What a decorator DOES to an ability — the one question two augments landing on the same ability
    /// are held up against each other by. It is the parameter the decorator stands on and the way it
    /// moves that parameter, and NEVER by how much: an augment cutting a fifth off the price and one
    /// cutting a third are the same offer at two strengths, so the amount is what decides which of them
    /// works and can be no part of what makes them the same thing. The rarity a copy rolled and the
    /// number it came out at are that amount by another name and are out for the same reason.
    ///
    /// The same question is answered for item lines by <see cref="Core.Modifiers.LineIdentity"/> — an
    /// atom is known by the stat it touches and the way it touches it, so "+35% damage" and "+33%
    /// damage" are one line. This is a type of its own rather than that one because the two are built
    /// out of different vocabularies: an item line is a modifier key with a channel and a predicate, an
    /// ability's effect is a string parameter key and a direction, and a shared type would be the union
    /// of both with neither half meaning anything on the other side.
    ///
    /// <para><b>What an augment gives and what it charges.</b> A record may ask for something in return
    /// for what it offers, and it asks in the two things a cast is paid with
    /// (<see cref="AbilityParameter.CastPrices"/>). A price belongs to the deal ONE record offers, so it
    /// carries that record's id: two copies of one record are one deal and are charged for once, while
    /// two records that each charge are two deals and both bills are paid. What an augment GIVES carries
    /// no source at all, which is the whole point — two different records reaching for the same
    /// parameter the same way are the same effect, and only the strongest of them works.</para>
    /// </summary>
    /// <param name="Parameter">The parameter key the decorator stands on.</param>
    /// <param name="Direction">The way it leaves that parameter.</param>
    /// <param name="Deal">The record a price belongs to; nothing at all for what an augment gives.</param>
    public readonly record struct AbilityEffectIdentity(string Parameter, AbilityEffectDirection Direction, string? Deal = null)
    {
        /// <summary>The identity of a seated decorator — the single place the question is answered.</summary>
        public static AbilityEffectIdentity Of(AbilityParameterDecorator decorator) =>
            IsPrice(decorator)
                ? new AbilityEffectIdentity(decorator.Parameter, decorator.Direction, decorator.Source)
                : new AbilityEffectIdentity(decorator.Parameter, decorator.Direction);

        /// <summary>Whether the decorator is what an augment charges rather than what it gives. Nothing
        /// on offer anywhere in the book is a higher price or a longer wait, so a move that raises one
        /// of those is the bill for something else the record does.</summary>
        private static bool IsPrice(AbilityParameterDecorator decorator) =>
            decorator.Direction == AbilityEffectDirection.Raise && AbilityParameter.CastPrices.Contains(decorator.Parameter);
    }
}
