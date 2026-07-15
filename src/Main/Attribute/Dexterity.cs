namespace LastBreath.Attribute
{
    using System.Collections.Generic;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Godot;

    public class Dexterity(IParameterModifiersComponent manager)
        : EntityAttribute(GetModifiers(), manager, new Modifier(ModifierValueType.Flat, EntityParameter.Dexterity, 0f))
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
            yield return new Modifier(ModifierValueType.Increase, EntityParameter.CriticalChance, 0.01f);

            yield return new Modifier(ModifierValueType.Increase, EntityParameter.CriticalDamage, 0.01f);

            yield return new Modifier(ModifierValueType.Increase, EntityParameter.AdditionalHitChance, 0.01f);

            yield return new Modifier(ModifierValueType.Increase, EntityParameter.Evade, 0.01f);
        }
    }
}
