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

        //public override void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default)
        //{

        //}
        public override void AfterBuffEnds(AttackComponent? attack = null, HealthComponent? health = null)
        {
            if(attack == null)
            {
                return;
            }

            attack.Leech = 0;
        }

        public override void BuffAttacks(AttackComponent? attack = null)
        {
            if (attack == null)
            {
                return;
            }

            attack.Leech = _leachPercentage;
        }

        public void Leech(AttackComponent? attack = null, HealthComponent? health = null)
        {
            if(attack == null || health == null)
            {
                return;
            }

            health.Heal(attack.LeechedHealth);
        }
    }
}
