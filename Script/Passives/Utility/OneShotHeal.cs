namespace Playground.Script.Passives.Attacks
{
    using System;

    public partial class OneShotHeal : Ability
    {
        private float _healAmoint = 50;
        public OneShotHeal()
        {
            
        }

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = null)
        {
            health.CurrentHealth += _healAmoint;
        }
        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null) => throw new NotImplementedException();

        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null) => throw new NotImplementedException();
    }
}
