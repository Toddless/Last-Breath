namespace Playground.Script.Passives.Attacks
{
    public partial class AdditionalAttack : Passive, IAttackPassives
    {
        public void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default, float amount = default)
        {

        }

        public void ApplyBeforeAttack(AttackComponent attack)
        {

        }
    }
}
