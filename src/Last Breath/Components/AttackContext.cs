namespace Playground.Components
{
    using System;
    using System.Collections.Generic;
    using Playground.Script;
    using Playground.Script.Abilities.Skills;

    public class AttackContext(ICharacter attaker, ICharacter target)
    {
        public ICharacter Attacker { get; } = attaker;
        public ICharacter Target { get; } = target;
        public float BaseDamage { get; set; }
        public float FinalDamage { get; set; }
        public bool IsCritical { get; set; }
        public List<ISkill> PassiveSkills { get; set; } = [];

        public event Action<AttackResult>? OnAttackResult;

        public void SetAttackResult(AttackResult result) => OnAttackResult?.Invoke(result);
    }
}
