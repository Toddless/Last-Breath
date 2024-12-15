namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class Regeneration : Ability, ICanHeal
    {
        private float _regenerationAmount = 15;
        public Regeneration()
        {
            BuffLasts = 3;
        }

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = null)
        {
            health.Heal(_regenerationAmount);
        }
        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => throw new NotImplementedException();

        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null) => throw new NotImplementedException();
    }
}
