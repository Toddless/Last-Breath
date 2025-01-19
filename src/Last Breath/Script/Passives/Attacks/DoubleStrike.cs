namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class DoubleStrike : Ability<AttackComponent>, ICanDealDamage
    {
        public DoubleStrike(AttackComponent component) : base(component)
        {
        }

        public override void AfterBuffEnds(AttackComponent? component) => throw new NotImplementedException();
        public override void ActivateAbility(AttackComponent? component) => throw new NotImplementedException();
        public override void EffectAfterAttack(AttackComponent? component) => throw new NotImplementedException();
    }
}
