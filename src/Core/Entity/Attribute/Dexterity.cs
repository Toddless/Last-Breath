namespace Core.Entity.Attribute
{
    using System.Collections.Generic;
    using Components;
    using Enums;
    using Modifiers;
    using Godot;

    public class Dexterity(IParameterModifiersComponent manager)
        : EntityAttribute(GetModifiers(), manager, new Modifier(ModifierValueType.Flat, EntityParameter.Dexterity, 0f))
    {
        private const float EvadePerPoint = 3f;

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
            if (parameter is not EntityParameter.Dexterity) return;
            Total = Mathf.RoundToInt(value);
        }


        /// <summary>Dexterity grants evasion and nothing else. One point is worth 1% of the unarmed evade
        /// base — the same share of its parameter that Strength and Intelligence give of theirs.</summary>
        private static IEnumerable<IModifier> GetModifiers() => [new Modifier(ModifierValueType.Flat, EntityParameter.Evade, EvadePerPoint)];
    }
}
