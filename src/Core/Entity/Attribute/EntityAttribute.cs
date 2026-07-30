namespace Core.Entity.Attribute
{
    using System;
    using System.Collections.Generic;
    using Components;
    using Enums;
    using Modifiers;
    using Godot;

    public abstract class EntityAttribute : IEntityAttribute
    {
        private readonly List<IModifierInstance> _instances = [];
        private readonly IParameterModifiersComponent _manager;
        private readonly IModifierInstance _investedAmountModifier;

        public abstract int Total { get; set; }

        public int InvestedPoints
        {
            get;
            private set
            {
                if (field.Equals(value)) return;
                field = value;
                UpdateInvestedAmount();
            }
        }


        public IReadOnlyCollection<IModifierInstance> Modifiers => _instances;

        protected EntityAttribute(IEnumerable<IModifier> modifiers, IParameterModifiersComponent manager, IModifier mod)
        {
            _manager = manager;
            string source = SourceOf(mod.EntityParameter);
            _investedAmountModifier = new SimpleModifier(mod.EntityParameter, mod.ModifierValueType, mod.Value, source);
            _manager.AddModifier(_investedAmountModifier);
            foreach (var modifier in modifiers)
            {
                var instance = new SimpleModifier(modifier.EntityParameter, modifier.ModifierValueType, modifier.Value, source);
                _instances.Add(instance);
            }
        }

        public virtual void IncreasePoints() => InvestedPoints++;
        public virtual void IncreasePointsByAmount(int amount) => InvestedPoints += amount;

        public virtual void DecreasePoints()
        {
            if (InvestedPoints == 0) return;
            InvestedPoints--;
        }

        public virtual void DecreasePointsByAmount(int amount)
        {
            if (InvestedPoints == 0) return;
            int value = Mathf.Min(InvestedPoints, amount);
            InvestedPoints -= value;
        }

        public abstract void OnParameterChanges(EntityParameter parameter, float value);

        protected void UpdateModifiers()
        {
            foreach (IModifierInstance modifier in _instances)
                modifier.Value = modifier.BaseValue * Total;

            _manager.UpdateModifiers(_instances);
        }

        /// <summary>Modifier source of an attribute, derived from the parameter that identifies it.
        /// Every modifier an attribute mints carries it, so <c>RemoveModifierBySource</c> drops the
        /// contribution of exactly one attribute; the source also has to stay stable across updates —
        /// <c>UpdateModifier(s)</c> matches an existing modifier by source and value type.</summary>
        private static string SourceOf(EntityParameter attribute) => $"Attribute_{attribute}";

        private void UpdateInvestedAmount()
        {
            float newValue = _investedAmountModifier.BaseValue + InvestedPoints;
            if (Math.Abs(_investedAmountModifier.Value - newValue) < 0.0001f) return;
            _investedAmountModifier.Value = newValue;
            _manager.UpdateModifier(_investedAmountModifier);
        }
    }
}
