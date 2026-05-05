namespace Core.Interfaces.Battle
{
    using Entity;
    using Enums;
    using Godot;

    public interface IAttackContext
    {
        RandomNumberGenerator Rnd { get; }
        IEntity Attacker { get; }
        IEntity Target { get; }
        IAttackContextScheduler AttackContextScheduler { get; }
        AttackResults Result { get; set; }
        float BaseDamage { get; }
        float RawCriticalChance { get; set; }
        float RawCriticalDamage { get; set; }
        float AdditionalDamage { get; set; }
        float FinalDamage { get; set; }
        bool IsCritical { get; set; }
        bool ForceCriticalAttack { get; set; }
        bool IsUnevadable { get; set; }
        bool IsUnblockable { get; set; }
        bool IsValid { get; }

        bool Schedule();
    }
}
