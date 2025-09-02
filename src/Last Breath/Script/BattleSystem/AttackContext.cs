namespace LastBreath.Script.BattleSystem
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Skills;

    public class AttackContext(ICharacter attaker, ICharacter target) : IAttackContext
    {
        public ICharacter Attacker { get; } = attaker;
        public ICharacter Target { get; } = target;
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
