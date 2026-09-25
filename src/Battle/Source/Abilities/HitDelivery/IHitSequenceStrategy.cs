namespace Battle.Source.Abilities.HitDelivery
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Entity;

    /// <summary>
    /// Delivery for direct-hit abilities (no attack pipeline): yields the sequence of targets the cast
    /// actually touches — one entry per hit, repeats allowed (a bounce can land on the same enemy twice).
    /// The ability hits each entry in order and fires its impact riders per hit.
    /// </summary>
    public interface IHitSequenceStrategy
    {
        IReadOnlyList<IFightable> GetHitSequence(IFightable owner, IReadOnlyList<IFightable> targets, IBattleField field);
    }
}
