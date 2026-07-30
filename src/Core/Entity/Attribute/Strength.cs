namespace Core.Entity.Attribute
{
    using System.Collections.Generic;
    using Components;
    using Enums;
    using Modifiers;
    using Godot;

    public class Strength(IParameterModifiersComponent manager) : EntityAttribute(GetEffects(), manager,  new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 0f))
    {
        private const float HealthPerPoint = 10f;

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
            if (parameter is not EntityParameter.Strength) return;
            Total = Mathf.RoundToInt(value);
        }

        /// <summary>Strength grants health and nothing else. One point is worth 1% of the unarmed health
        /// base — the same share of its parameter that Dexterity and Intelligence give of theirs.</summary>
        private static IEnumerable<IModifier> GetEffects() => [new Modifier(ModifierValueType.Flat, EntityParameter.Health, HealthPerPoint)];
    }
}
