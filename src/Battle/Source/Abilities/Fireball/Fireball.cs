namespace Battle.Source.Abilities.Fireball
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Entity;

    // dead ability. need attention later
    public class Fireball(AbilityBaseData data) : DamagingAbility(data)
    {
        public override IAbility Copy() => CopyUpgradesTo(new Fireball(Data));

        protected override Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field) => throw new System.NotImplementedException();
    }
}
