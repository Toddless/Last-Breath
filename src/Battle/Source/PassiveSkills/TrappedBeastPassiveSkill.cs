namespace Battle.Source.PassiveSkills
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Modifiers;

    public class TrappedBeastPassiveSkill : Skill
    {
        private IModifierInstance _increaseDamageModifier;

        public TrappedBeastPassiveSkill(float healthPercent, float damageBonus) : base(id: "Passive_Skill_Trapped_Beast")
        {
            HealthPercent = healthPercent;
            DamageBonus = damageBonus;
            _increaseDamageModifier = new ModifierInstance(EntityParameter.Damage, ModifierType.Increase, 0f, this);
        }

        public float HealthPercent { get; }
        public float DamageBonus { get; }

        public override void Attach(IEntity owner)
        {
            Owner = owner;
            Owner.CurrentHealthChanged += OnCurrentHealthChanged;
        }

        private void OnCurrentHealthChanged(float currentHealth)
        {
            if (Owner == null) return;
            float percentLost = 1 - (currentHealth / Owner.Parameters.MaxHealth);
            int steps = (int)(percentLost / DamageBonus);
            float bonus = 1f + steps * DamageBonus;
            _increaseDamageModifier.Value = bonus;
            Owner.Modifiers.UpdatePermanentModifier(_increaseDamageModifier);
        }

        public override void Detach(IEntity owner)
        {
            Owner?.Modifiers.RemovePermanentModifier(_increaseDamageModifier);
            Owner?.CurrentHealthChanged -= OnCurrentHealthChanged;
            Owner = null;
        }

        public override ISkill Copy() => new TrappedBeastPassiveSkill(HealthPercent, DamageBonus);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not TrappedBeastPassiveSkill beast) return false;

            return beast.HealthPercent > HealthPercent;
        }
    }
}
