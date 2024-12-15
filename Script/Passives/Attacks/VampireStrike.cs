namespace Playground.Script.Passives.Attacks
{
    using Playground.Script.Passives.Interfaces;

    public partial class VampireStrike : Ability, ICanLeech, ICanBuffAttack
    {
        private float _leachPercentage = 0.1f;

        public VampireStrike()
        {
            HaveISomethinToApplyAfterAttack = true;
        }

        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null)
        {
            if(attack == null)
            {
                return;
            }

            attack.Leech = 0;
        }

        public override void ActivateAbility(AttackComponent? attack = null, HealthComponent? health = null)
        {
            if (attack == null)
            {
                return;
            }

            attack.Leech = _leachPercentage;
        }

        public override void EffectAfterAttack(AttackComponent? attack = null, HealthComponent? health = null)
        {
            health.Heal(attack.LeechedHealth);
            attack.LeechedHealth = 0;
        }
    }
}
