namespace Battle.Attribute
{
    using Godot;
    using System;
    using Core.Enums;
    using Core.Modifiers;
    using System.Collections.Generic;
    using Core.Interfaces.Components;

    public abstract class EntityAttribute : IEntityAttribute
    {
        // TODO: Add new effect to attributes (e.g int will give the player increase spell damage) as
        private readonly List<IModifierInstance> _instances = [];
        private readonly IModifiersComponent _manager;
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

        protected EntityAttribute(IEnumerable<IModifier> modifiers, IModifiersComponent manager, IModifier mod)
        {
            _manager = manager;
            _investedAmountModifier = new ModifierInstance(mod.EntityParameter, mod.ModifierType, mod.Value, this);
            _manager.AddPermanentModifier(_investedAmountModifier);
            foreach (var modifier in modifiers)
            {
                var instance = new ModifierInstance(modifier.EntityParameter, modifier.ModifierType, modifier.Value, this);
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

            _manager.UpdatePermanentModifiers(_instances);
        }

        private void UpdateInvestedAmount()
        {
            float newValue = _investedAmountModifier.BaseValue + InvestedPoints;
            if (Math.Abs(_investedAmountModifier.Value - newValue) < 0.0001f) return;
            _investedAmountModifier.Value = newValue;
            _manager.UpdatePermanentModifier(_investedAmountModifier);
        }
    }
}
