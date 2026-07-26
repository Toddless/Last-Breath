namespace Core.Entity.Attribute
{
    using System.Collections.Generic;
    using Components;
    using Enums;
    using Modifiers;
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
            yield return new Modifier(ModifierValueType.Flat, EntityParameter.PhysicalDamage, 5f);

            yield return new Modifier(ModifierValueType.Flat, EntityParameter.Armor, 50f);

            yield return new Modifier(ModifierValueType.Increase, EntityParameter.HealthRecovery, 0.01f);

            yield return new Modifier(ModifierValueType.Flat, EntityParameter.Health, 10f);
        }
    }
}
