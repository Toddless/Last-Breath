namespace Battle.Attribute
{
    using Godot;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public class Strength(IModifierManager manager) : EntityAttribute(GetEffects(), manager,  new Modifier(ModifierType.Flat, EntityParameter.Strength, 1f))
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
            if (parameter is not EntityParameter.Intelligence) return;
            Total = Mathf.RoundToInt(value);
        }

        private static IEnumerable<IModifier> GetEffects()
        {
            yield return new Modifier(ModifierType.Flat, EntityParameter.Damage, 15f);

            yield return new Modifier(ModifierType.Flat, EntityParameter.Armor, 100f);

            yield return new Modifier(ModifierType.Increase, EntityParameter.Armor, 0.02f);

            yield return new Modifier(ModifierType.Flat, EntityParameter.Health, 10f);
        }
    }
}
