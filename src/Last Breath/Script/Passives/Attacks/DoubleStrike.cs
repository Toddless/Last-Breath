namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Passives.Interfaces;

    public partial class DoubleStrike : Ability, ICanDealDamage
    {
        public override Type TargetTypeComponent => typeof(AttackComponent);

        public DoubleStrike()
        {
        }

        public override void AfterBuffEnds(IGameComponent? component) => throw new System.NotImplementedException();

        public override void ActivateAbility(IGameComponent? component) => throw new System.NotImplementedException();
        public override void EffectAfterAttack(IGameComponent? component) => throw new System.NotImplementedException();
    }
}
