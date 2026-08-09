namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Entity;
    using Effects;

    public interface IPoisonSpreadMode
    {
        /// <summary>
        /// Carries the exploded stacks onward and answers with everyone the spread actually reached.
        /// The answer is not bookkeeping: each of those fighters was touched by the cast, and a touch
        /// nobody reports is a touch no impact rider will ever see.
        /// </summary>
        public IReadOnlyList<IFightable> SpreadPoison(
            List<DamageOverTurnEffect> originalStacks, IFightable originalTarget, IFightable owner, IBattleField field, string source);
    }
}
