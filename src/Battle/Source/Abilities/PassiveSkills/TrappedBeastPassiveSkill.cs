namespace Battle.Source.Abilities.PassiveSkills
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Modifiers;

    public class TrappedBeastPassiveSkill : Skill
    {
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
            Owner = owner;
            Owner.CurrentHealthChanged += OnCurrentHealthChanged;
        }

        private void OnCurrentHealthChanged(float currentHealth)
        {
            if (Owner == null) return;
            float percentLost = 1 - (currentHealth / Owner.Parameters.MaxHealth);
            int steps = (int)(percentLost / PercentBonus);
            float bonus = 1f + steps * PercentBonus;
            _increaseDamageModifier.Value = bonus;
            Owner.Modifiers.UpdatePermanentModifier(_increaseDamageModifier);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.Modifiers.RemovePermanentModifier(_increaseDamageModifier);
            Owner?.CurrentHealthChanged -= OnCurrentHealthChanged;
            Owner = null;
        }

        public override ISkill Copy() => new TrappedBeastPassiveSkill(Id, PercentHealth, PercentBonus);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not TrappedBeastPassiveSkill beast) return false;

            return beast.PercentHealth > PercentHealth;
        }
    }
}
