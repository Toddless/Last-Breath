namespace Core.Interfaces.Battle
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;

    public interface IAttackContext
    {
        float Armor { get; set; }
        IEntity Attacker { get; }
        float CriticalDamageMultiplier { get; set; }
        float Damage { get; set; }
        float FinalDamage { get; set; }
        bool IgnoreArmor { get; set; }
        bool IgnoreBarrier { get; set; }
        bool IgnoreEvade { get; set; }
        bool IsCritical { get; set; }
        float MaxReduceDamage { get; set; }
        List<ISkill> PassiveSkills { get; set; }
        IEntity Target { get; }

        event Action<IAttackContext>? OnAttackCanceled;
        event Action<IAttackResult>? OnAttackResult;

        void CancelAttack();
        void SetAttackResult(IAttackResult result);
    }
}
