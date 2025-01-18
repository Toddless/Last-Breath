namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Passives.Interfaces;

    public partial class VampireStrike : Ability, ICanLeech, ICanBuffAttack
    {
        private float _leachPercentage = 0.1f;

        public override Type TargetTypeComponent => typeof(AttackComponent);

        public VampireStrike()
        {
            HaveISomethinToApplyAfterAttack = true;
        }

        public override void AfterBuffEnds(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            attack.Leech = 0;
        }

        public override void ActivateAbility(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }

            attack.Leech = _leachPercentage;
        }

        public override void EffectAfterAttack(IGameComponent? component)
        {
            if (component == null || component is not HealthComponent health)
            {
                return;
            }
        }
    }
}
