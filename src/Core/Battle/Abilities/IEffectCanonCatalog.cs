namespace Core.Battle.Abilities
{
    using System.Collections.Generic;

    /// <summary>
    /// The canonical numbers of every temporary effect as <c>SharedData/Effects</c> balances them. What a
    /// row SAYS is not a battle question — a card, an editor and a tooltip ask it in compositions that
    /// build no battle module — while what builds an effect out of those numbers stays with the registry.
    /// </summary>
    public interface IEffectCanonCatalog
    {
        /// <summary>Every effect id the canon carries a row for.</summary>
        IReadOnlyCollection<string> Ids { get; }

        /// <summary>The canonical figures of an effect, or null for an id the canon carries no row for.</summary>
        IReadOnlyDictionary<string, float>? CanonOf(string effectId);

        /// <summary>The strength the row names, and Weak for an id the canon carries no row for — which
        /// is also what every effect is until a row names it otherwise.</summary>
        EffectPower PowerOf(string effectId);

        /// <summary>How many stacks the canon balances an effect at, or null when no row names one.</summary>
        int? StackCeilingOf(string effectId);
    }
}
