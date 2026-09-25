namespace Battle.Source.Abilities.HitDelivery
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Entity;

    /// <summary>Default delivery: one hit per selected target, in selection order.</summary>
    public sealed class SelectedTargetsHits : IHitSequenceStrategy
    {
        public IReadOnlyList<IFightable> GetHitSequence(IFightable owner, IReadOnlyList<IFightable> targets, IBattleField field) =>
            targets.Where(target => target.IsAlive).ToList();
    }
}
