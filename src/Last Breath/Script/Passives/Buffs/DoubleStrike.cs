namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class DoubleStrike : Ability<AttackComponent, BaseEnemy>, ICanDealDamage
    {
        public override void ActivateAbility(AttackComponent? component) => throw new NotImplementedException();
        public override void SetTargetCharacter(BaseEnemy? target) => throw new NotImplementedException();
    }
}
