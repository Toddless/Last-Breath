namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Passives.Interfaces;

    public partial class Regeneration : Ability, ICanHeal
    {
        private float _regenerationAmount = 15;

        public Regeneration()
        {
            BuffLasts = 3;
        }

        public override Type TargetTypeComponent => typeof(HealthComponent);

        public override void ActivateAbility(IGameComponent? component)
        {
            if (component == null || component is not HealthComponent health)
            {
                return;
            }
            health.CurrentHealth += _regenerationAmount;
        }

        public override void AfterBuffEnds(IGameComponent? component)
        {
            if (component == null || component is not AttackComponent attack)
            {
                return;
            }
            BuffLasts = 3;
        }

        public override void EffectAfterAttack(IGameComponent? component)
        {
            return;
        }
    }
}
