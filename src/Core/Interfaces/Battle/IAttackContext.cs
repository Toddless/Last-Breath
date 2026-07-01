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

        /// <summary>
        ///  <c>TakeDamage</c>: overwritten with the damage actually dealt to the target
        /// (post incoming-mitigation, barrier-absorbed portion included). Post-attack reactions
        /// (leech, damage-scaled DoTs, splash) should read it after the hit is applied.
        /// </summary>
        float FinalDamage { get; set; }

        bool IsCritical { get; set; }
        bool ForceCriticalAttack { get; set; }
        bool IsUnevadable { get; set; }
        bool IsUnblockable { get; set; }
        bool IsValid { get; }

        /// <summary>Position of this attack within a multi-hit sequence (folded-in AttackMetadata). Single attacks: Index 0, TotalCount 1.</summary>
        int Index { get; set; }
        int TotalCount { get; set; }
        bool IsFirst { get; }
        bool IsLast { get; }

        bool Schedule();
    }
}
