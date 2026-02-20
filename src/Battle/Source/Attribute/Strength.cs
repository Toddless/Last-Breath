namespace Battle.Source.Attribute
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Core.Modifiers;
    using Godot;

    public class Strength(IModifiersComponent manager) : EntityAttribute(GetEffects(), manager,  new Modifier(ModifierType.Flat, EntityParameter.Strength, 0f))
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
            yield return new Modifier(ModifierType.Flat, EntityParameter.Damage, 15f);

            yield return new Modifier(ModifierType.Flat, EntityParameter.Armor, 100f);

            yield return new Modifier(ModifierType.Increase, EntityParameter.HealthRecovery, 0.01f);

            yield return new Modifier(ModifierType.Flat, EntityParameter.Health, 10f);
        }
    }
}
