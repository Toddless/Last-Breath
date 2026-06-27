namespace Battle.Source.Abilities.PoisonExplosion
{
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Effects;

    public interface IPoisonSpreadMode
    {
            public void SpreadPoison(List<DamageOverTurnEffect> originalStacks, IEntity originalTarget, IEntity owner, IBattleField field, string source);

    }
}
