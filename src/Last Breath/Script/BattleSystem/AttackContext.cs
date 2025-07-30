namespace LastBreath.Script.BattleSystem
{
    using System;
    using System.Collections.Generic;
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Interfaces;

    public class AttackContext(ICharacter attaker, ICharacter target)
    {
        public ICharacter Attacker { get; } = attaker;
        public ICharacter Target { get; } = target;
        public float Damage { get; set; }
        public float CriticalDamageMultiplier { get; set; }
        public float Armor { get; set; }
        public float MaxReduceDamage { get; set; }
        public float FinalDamage {  get; set; }
        public bool IsCritical { get; set; }
        public bool IgnoreEvade { get; set; }
        public bool IgnoreArmor {  get; set; }
        public bool IgnoreBarrier { get; set; }
        public List<ISkill> PassiveSkills { get; set; } = [];

        public event Action<AttackResult>? OnAttackResult;
        public event Action<AttackContext>? OnAttackCanceled;

        public void SetAttackResult(AttackResult result) => OnAttackResult?.Invoke(result);
        public void CancelAttack() => OnAttackCanceled?.Invoke(this);
    }
}
