namespace Battle.Internal.Attribute
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Core.Modifiers;
    using Godot;

    public class Strength(IParameterModifiersComponent manager) : EntityAttribute(GetEffects(), manager,  new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 0f))
    {
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

        private static IEnumerable<IModifier> GetEffects()
        {
            yield return new Modifier(ModifierValueType.Flat, EntityParameter.Damage, 15f);

            yield return new Modifier(ModifierValueType.Flat, EntityParameter.Armor, 100f);

            yield return new Modifier(ModifierValueType.Increase, EntityParameter.HealthRecovery, 0.01f);

            yield return new Modifier(ModifierValueType.Flat, EntityParameter.Health, 10f);
        }
    }
}
