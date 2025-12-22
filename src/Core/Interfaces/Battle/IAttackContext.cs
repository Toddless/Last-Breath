namespace Core.Interfaces.Battle
{
    using Entity;
    using Components;

    public interface IAttackContext
    {
        IRandomNumberGenerator Rnd { get; }
        IEntity Attacker { get; }
        IEntity Target { get; }
        IAttackContextScheduler AttackContextScheduler { get; }
        float BaseDamage { get; }
        float AdditionalDamage { get; set; }
        float FinalDamage { get; set; }
        bool IsCritical { get; set; }
        bool IsAttackSucceed { get; set; }

        void Schedule();
    }
}
