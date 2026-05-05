namespace Battle.Source.Abilities
{
    using System.Linq;
    using System.Collections.Generic;

    public class SinglePowerfulHitTransformer : IAttackSeriesTransformer
    {
        public IEnumerable<AttackEntry> Transform(IEnumerable<AttackEntry> attacks)
        {
            float total = attacks.Sum(entry => entry.AttackDamage);
            yield return new AttackEntry(total);
        }
    }
}
