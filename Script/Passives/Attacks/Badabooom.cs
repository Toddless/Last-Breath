namespace Playground.Script.Passives.Attacks
{
    using System;
    using Playground.Script.Passives.Interfaces;

    public partial class Badabooom : Ability, ICanDealDamage
    {
        private float _damage;

        public Badabooom()
        {
            Cooldown = 4;
        }
        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = null)
        {
        }
        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => throw new NotImplementedException();
        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null) => throw new NotImplementedException();
    }
}
