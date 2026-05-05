namespace Battle.Source.Abilities
{
    using System.Collections.Generic;

    public interface IAttackSeriesTransformer
    {
        IEnumerable<AttackEntry> Transform(IEnumerable<AttackEntry> attacks);
    }
}
