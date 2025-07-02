namespace Playground.Script.Abilities.Skills
{
    using System;
    using Godot;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;

    public class OnEdge : ISkill
    {
        private ICharacter? _owner;
        private float _increaseDamagePerBonusPoint;
        private float _percentDamagePerMissingPercentHealth;
        private IModifier _modifier;

        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public SkillType SkillType { get; private set; } = SkillType.DamageTaken;
        public Texture2D? Icon { get; private set; }

        public OnEdge()
        {
            LoadData();
            _modifier = new DamageModifier(ModifierType.Increase, CalculateValue(), this, ModifierPriorities.Buffs);
        }
        public void OnObtaining(ICharacter owner)
        {
            _owner = owner;
            UpdateModifierValue();
            _owner.Modifiers.AddPermanentModifier(_modifier);
            _owner.Health.CurrentHealthChanged += OnHealthChanged;
        }

        public void OnLoss()
        {
            if (_owner != null)
            {
                _owner.Health.CurrentHealthChanged -= OnHealthChanged;
                _owner.Modifiers.RemovePermanentModifier(_modifier);
                _owner = null;
            }
        }

        private void OnHealthChanged(float obj)
        {
            UpdateModifierValue();
            _owner!.Modifiers.UpdatePermanentModifier(_modifier);
        }

        private void UpdateModifierValue() => _modifier.Value = CalculateValue();

        private float CalculateValue()
        {
            try
            {
                if (_owner == null) return 0;
                var missingHealthPercent = 1 - (_owner.Health.CurrentHealth / _owner.Health.MaxHealth);
                // calculation for bonuses (e.g each 5% missing health give us 3% increase damage
                // so we divide for example 70 / 5 = 14 damage increases)
                var totalBonus = missingHealthPercent / _percentDamagePerMissingPercentHealth;
                // total increase bonus damage (14 * 3 = 42%)
                return totalBonus * _increaseDamagePerBonusPoint;
            }
            catch (DivideByZeroException)
            {
                return 0;
            }
        }

        private void LoadData()
        {

        }
    }
}
