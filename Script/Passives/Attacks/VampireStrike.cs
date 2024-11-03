namespace Playground.Script.Passives.Attacks
{
    public partial class VampireStrike : Passive, IAttackPassives
    {
        private float _leachPercentage = 0.1f;

        public float LeechPercentage
        {
            get => _leachPercentage;
            set => _leachPercentage = value;
        }

        public void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default, float dealedDamage = default)
        {
            if(health != null && dealedDamage != 0)
            {
                health.Heal(dealedDamage * _leachPercentage);
            }
        }

        public void ApplyBeforeAttack(AttackComponent attack)
        {
        }
    }
}
