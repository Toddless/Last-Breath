namespace Playground.Script.Attribute
{
    using System;
    using System.Collections.Generic;
    using Playground.Components.Interfaces;
    using Playground.Script.Helpers;

    public abstract class AttributeBase(IEnumerable<AttributeEffect> effects) : IAttribute
    {
        private const int BaseValue = 1;
        private int _investedPoints;
        private readonly List<AttributeEffect> _effects = [.. effects];

        public event Action<IAttribute>? AttributeChanged;

        public int InvestedPoints
        {
            get => _investedPoints;
            private set
            {
                if (ObservableProperty.SetProperty(ref _investedPoints, value))
                    AttributeChanged?.Invoke(this);
            }

        }

        public virtual void IncreasePoints() => InvestedPoints++;

        public virtual void InscreasePointsByAmount(int amount) => InvestedPoints += amount;

        public virtual void DecreasePoints() => InvestedPoints--;

        public virtual void DescreasePointsByAmount(int amount) => InvestedPoints -= amount;


        public List<AttributeModifier> AttributeModifiers()
        {
            List<AttributeModifier> modifiers = [];

            foreach (var effect in _effects)
            {
                var modifier = new AttributeModifier(
                    effect.TargetParameter,
                    (BaseValue + _investedPoints) * effect.ValuePerPoint,
                    effect.ModifierType,
                    this,
                    effect.Priority);

                modifiers.Add(modifier);
            }
            return modifiers;
        }
    }
}
