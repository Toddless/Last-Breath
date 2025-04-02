namespace Playground.Script.Attribute
{
    using System.Collections.Generic;
    using System.Linq;
    using Playground.Components;
    using Playground.Components.Interfaces;
    using Playground.Script.Helpers;

    public abstract class AttributeBase(IEnumerable<AttributeEffect> effects, ModifierManager manager) : ObservableProperty, IAttribute
    {
        private const int BaseValue = 1;
        private int _investedPoints;
        private readonly List<AttributeEffect> _effects = effects.ToList();
        private readonly ModifierManager _modifierManager = manager;

        public int InvestedPoints
        {
            get => _investedPoints;
            set
            {
                if (SetProperty(ref _investedPoints, value))
                {
                    UpdateModifiers();
                }
            }
        }

        public virtual void UpdateModifiers()
        {
            RemoveAllModifiers();
            foreach (var effect in _effects)
            {
                var modifier = new AttributeModifier
                    (effect.TargetParameter,
                    (BaseValue + _investedPoints) * effect.ValuePerPoint,
                    effect.ModifierType,
                    this,
                    effect.Priority
                    );

                _modifierManager.AddPermanentModifier(modifier);
            }
        }

        private void RemoveAllModifiers()
        {
            var modifiersToRemove = _modifierManager.PermanentModifiers
                .SelectMany(x => x.Value)
                .OfType<AttributeModifier>()
                .Where(x => x.SourceAttribute == this)
                .ToList();

            modifiersToRemove.ForEach(_modifierManager.RemovePermanentModifier);
        }
    }
}
