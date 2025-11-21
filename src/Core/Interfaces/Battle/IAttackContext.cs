namespace Core.Interfaces.Battle
{
    using System;
    using Entity;
    using Skills;
    using System.Collections.Generic;

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
