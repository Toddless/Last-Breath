namespace Battle
{
    using System;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using System.Collections.Generic;

    public class AttackContext(IEntity attaker, IEntity target) : IAttackContext
    {
        public IEntity Attacker { get; } = attaker;
        public IEntity Target { get; } = target;
        public float Damage { get; set; }
        public float CriticalDamageMultiplier { get; set; }
        public float Armor { get; set; }
        public float MaxReduceDamage { get; set; }
        public float FinalDamage { get; set; }
        public bool IsCritical { get; set; }
        public bool IgnoreEvade { get; set; }
        public bool IgnoreArmor { get; set; }
        public bool IgnoreBarrier { get; set; }
        public List<ISkill> PassiveSkills { get; set; } = [];

        public event Action<IAttackResult>? OnAttackResult;
        public event Action<IAttackContext>? OnAttackCanceled;

        public void SetAttackResult(IAttackResult result) => OnAttackResult?.Invoke(result);
        public void CancelAttack() => OnAttackCanceled?.Invoke(this);
    }
}
