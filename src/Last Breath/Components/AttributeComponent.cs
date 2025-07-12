namespace Playground.Components
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Attribute;
    using Playground.Script.Enums;

    public class AttributeComponent
    {
        private readonly Dexterity _dexterity;
        private readonly Strength _strength;
        private readonly Intelligence _intelligence;

        public Action<IModifier>? CallModifierManager;

        public AttributeComponent()
        {
            _dexterity = new();
            _strength = new();
            _intelligence = new();
            SetEvents();
        }


        public void InscreaseAttribute(Parameter attribute)
        {
            switch (attribute)
            {
                case Parameter.Dexterity: _dexterity.IncreasePoints(); break;
                case Parameter.Strength: _strength.IncreasePoints(); break;
                case Parameter.Intelligence: _intelligence.IncreasePoints(); break;
                default: break;
            }
        }

        public void IncreaseAttributeByAmount(Parameter attribute, int amount)
        {
            switch (attribute)
            {
                case Parameter.Dexterity: _dexterity.InscreasePointsByAmount(amount); break;
                case Parameter.Strength: _strength.InscreasePointsByAmount(amount); break;
                case Parameter.Intelligence: _intelligence.InscreasePointsByAmount(amount); break;
                default: break;
            }
        }

        public void DecreaseAttributeByAmount(Parameter attribute, int amount)
        {
            switch (attribute)
            {
                case Parameter.Dexterity: _dexterity.DescreasePointsByAmount(amount); break;
                case Parameter.Strength: _strength.DescreasePointsByAmount(amount); break;
                case Parameter.Intelligence: _intelligence.DescreasePointsByAmount(amount); break;
                default: break;
            }
        }

        public void DecreaseAttribute(Parameter attribute)
        {
            switch (attribute)
            {
                case Parameter.Dexterity: _dexterity.DecreasePoints(); break;
                case Parameter.Strength: _strength.DecreasePoints(); break;
                case Parameter.Intelligence: _intelligence.DecreasePoints(); break;
                default: break;
            }
        }

        private void UpdateModifiers(IAttribute attribute)
        {
            foreach (var modifier in attribute.AttributeModifiers())
            {
                CallModifierManager?.Invoke(modifier);
            }
        }

        private void SetEvents()
        {
            _dexterity.AttributeChanged += UpdateModifiers;
            _strength.AttributeChanged += UpdateModifiers;
            _intelligence.AttributeChanged += UpdateModifiers;
        }
    }
}
