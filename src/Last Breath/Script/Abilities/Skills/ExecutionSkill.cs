namespace LastBreath.Script.Abilities.Skills
{
    using System;
    using Godot;
    using Core.Enums;
    using Core.Interfaces;

    [GlobalClass]
    public partial class ExecutionSkill : Resource
    {
        private readonly Lazy<string> _id;
        private const float Trashhold = 0.15f;
        public string Id => _id.Value;
        public SkillType Type => SkillType.OnAttack;
        public bool IsEvadable => false;

        public ExecutionSkill()
        {
            _id = new(CreateId);
        }

        public void Activate(ICharacter target)
        {
            if (target.Health.CurrentHealth / target.Health.MaxHealth <= Trashhold) target.TakeDamage(int.MaxValue);
        }

        private string CreateId() => $"{GetType().Name}_{Type}";
    }
}
