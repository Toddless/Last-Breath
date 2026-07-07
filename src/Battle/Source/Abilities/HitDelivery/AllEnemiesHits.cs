namespace Battle.Source.Abilities.HitDelivery
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Entity;

    /// <summary>Global delivery: one hit on every living enemy on the field, ignoring the selection.</summary>
    public sealed class AllEnemiesHits : IHitSequenceStrategy
    {
        public IReadOnlyList<IFightable> GetHitSequence(IFightable owner, IReadOnlyList<IFightable> targets, IBattleField field) =>
            field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
    }
}
