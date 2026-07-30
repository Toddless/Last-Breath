namespace Core.Entity.Attribute
{
    using System.Collections.Generic;
    using Components;
    using Enums;
    using Modifiers;
    using Godot;

    public class Intelligence(IParameterModifiersComponent manager) :
        EntityAttribute(GetEffects(), manager,  new Modifier(ModifierValueType.Flat, EntityParameter.Intelligence, 0f))
    {
        private const float ManaPerPoint = 5f;

        public override int Total
        {
            get;
            set
            {
                if (field.Equals(value)) return;
                field = value;
                UpdateModifiers();
            }
        }

        public override void OnParameterChanges(EntityParameter parameter, float value)
        {
            if (parameter is not EntityParameter.Intelligence) return;
            Total = Mathf.RoundToInt(value);
        }

        /// <summary>Intelligence grants mana and nothing else. One point is worth 1% of the unarmed mana
        /// base — the same share of its parameter that Strength and Dexterity give of theirs.</summary>
        private static IEnumerable<IModifier> GetEffects() => [new Modifier(ModifierValueType.Flat, EntityParameter.Mana, ManaPerPoint)];
    }
}
