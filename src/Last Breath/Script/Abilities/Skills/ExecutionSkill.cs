namespace Playground.Script.Abilities.Skills
{
    using System;
    using Godot;
    using Playground.Localization;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Enums;

    [GlobalClass]
    public partial class ExecutionSkill : Resource, IOnAttackSkill
    {
        private readonly Lazy<string> _id;
        private const float Trashhold = 0.15f;
        public string Id => _id.Value;
        public SkillType Type => SkillType.OnAttack;
        public bool IsEvadable => false;

        [Export] public LocalizedString? Description { get; set; }

        [Export] public Texture2D? Icon { get; set; }

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
