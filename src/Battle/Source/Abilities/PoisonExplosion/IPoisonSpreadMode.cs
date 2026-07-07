namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using Core.Battle;
    using Core.Entity;
    using Effects;

    public interface IPoisonSpreadMode
    {
        public void SpreadPoison(List<DamageOverTurnEffect> originalStacks, IFightable originalTarget, IFightable owner, IBattleField field, string source);
    }
}
