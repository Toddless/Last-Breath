namespace Playground.Script.Passives.Attacks
{
    public interface IAttackPassives
    {
        void ApplyBeforeAttack(AttackComponent attack);

        void ApplyAfterAttack(AttackComponent? attack = default, HealthComponent? health = default, float dealedDamage = default);
    }
}
