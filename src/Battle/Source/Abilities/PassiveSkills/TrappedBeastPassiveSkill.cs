namespace Battle.Source.Abilities.PassiveSkills
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Modifiers;
    using Godot;

    public class TrappedBeastPassiveSkill : Skill
    {
        private IEntity? _owner;
        private IModifierInstance _increaseDamageModifier;

        public TrappedBeastPassiveSkill(string id, float percentHealth, float percentBonus) : base(id)
        {
            PercentHealth = percentHealth;
            PercentBonus = percentBonus;
            _increaseDamageModifier = new ModifierInstance(EntityParameter.Damage, ModifierType.Increase, 0f, this);
        }

        public float PercentHealth { get; }
        public float PercentBonus { get; }

        public override void Attach(IEntity owner)
        {
            _owner = owner;
            owner.CurrentHealthChanged += OnCurrentHealthChanged;
        }

        private void OnCurrentHealthChanged(float currentHealth)
        {
            if (_owner == null) return;
            float percentLost = 1 - (currentHealth / _owner.Parameters.MaxHealth);
            int steps = (int)(percentLost / PercentBonus);
            float bonus = 1f + steps * PercentBonus;
            _increaseDamageModifier.Value = bonus;
            _owner.Modifiers.UpdatePermanentModifier(_increaseDamageModifier);
        }

        public override void Detach(IEntity owner)
        {
            owner.Modifiers.RemovePermanentModifier(_increaseDamageModifier);
            owner.CurrentHealthChanged -= OnCurrentHealthChanged;
            _owner = null;
        }

        public override ISkill Copy() => new TrappedBeastPassiveSkill(Id, PercentHealth, PercentBonus);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not TrappedBeastPassiveSkill beast) return false;

            return beast.PercentHealth > PercentHealth;
        }
    }
}
