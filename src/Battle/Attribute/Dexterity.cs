namespace Battle.Attribute
{
    using Godot;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces;
    using System.Collections.Generic;
    using Core.Interfaces.Components;

    public class Dexterity(IModifierManager manager) : EntityAttribute(GetModifiers(), manager, new Modifier(ModifierType.Flat, EntityParameter.Dexterity, 1f))
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
            if (parameter is not EntityParameter.Dexterity) return;
            Total = Mathf.RoundToInt(value);
        }


        private static IEnumerable<IModifier> GetModifiers()
        {
            yield return new Modifier(ModifierType.Increase, EntityParameter.CriticalChance, 0.05f);

            yield return new Modifier(ModifierType.Increase, EntityParameter.CriticalDamage, 0.1f);

            yield return new Modifier(ModifierType.Increase, EntityParameter.AdditionalHitChance, 0.1f);

            yield return new Modifier(ModifierType.Increase, EntityParameter.Evade, 0.1f);
        }
    }
}
