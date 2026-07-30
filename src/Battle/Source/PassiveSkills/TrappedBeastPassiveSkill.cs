namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Modifiers;

    public class TrappedBeastPassiveSkill : Skill
    {
        private IModifierInstance _increaseDamageModifier;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(HealthPercent)] = HealthPercent,
                    [nameof(DamageBonus)] = DamageBonus,
                };
                return field;
            }
        }

        public TrappedBeastPassiveSkill(float healthPercent, float damageBonus) : base(id: "Passive_Skill_Trapped_Beast")
        {
            HealthPercent = healthPercent;
            DamageBonus = damageBonus;
            _increaseDamageModifier = new SimpleModifier(EntityParameter.PhysicalDamage, ModifierValueType.Increase, 0f, InstanceId);
        }

        public float HealthPercent { get; }
        public float DamageBonus { get; }

        /// <summary>Strength measure for collisions: both fields drive the effect and pull in opposite
        /// directions — a SMALLER step (<see cref="HealthPercent"/>) fires more often, a bigger
        /// <see cref="DamageBonus"/> pays more per step. Their ratio is the damage gained per unit
        /// of missing health.</summary>
        private float BonusPerLostHealth => HealthPercent > 0 ? DamageBonus / HealthPercent : 0;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CurrentHealthChanged += OnCurrentHealthChanged;
        }

        private void OnCurrentHealthChanged(float currentHealth)
        {
            if (Owner == null) return;
            float percentLost = 1 - (currentHealth / Owner.Parameters.MaxHealth);
            // Steps are counted by missing-health chunks; each step adds DamageBonus as an Increase
            // (the component already sums increases on top of 1 — no manual "1 +" here).
            int steps = (int)(percentLost / HealthPercent);
            _increaseDamageModifier.Value = steps * DamageBonus;
            Owner.ParameterModifiers.UpdateModifier(_increaseDamageModifier);
        }

        public override void Detach(IFightable owner)
        {
            Owner?.ParameterModifiers.RemoveModifier(_increaseDamageModifier);
            Owner?.CurrentHealthChanged -= OnCurrentHealthChanged;
            Owner = null;
        }

        public override ISkill Copy() => new TrappedBeastPassiveSkill(HealthPercent, DamageBonus);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not TrappedBeastPassiveSkill beast) return false;

            return BonusPerLostHealth > beast.BonusPerLostHealth;
        }
    }
}
