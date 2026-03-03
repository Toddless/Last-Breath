namespace LastBreath.Source.Attribute
{
    using System.Collections.Generic;
    using Battle.Source.Attribute;
    using Core.Enums;
    using Core.Interfaces.Components;
    using Core.Modifiers;
    using Godot;

    public class Dexterity(IModifiersComponent manager)
        : EntityAttribute(GetModifiers(), manager, new Modifier(ModifierType.Flat, EntityParameter.Dexterity, 0f))
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

            yield return new Modifier(ModifierType.Increase, EntityParameter.AdditionalHitChance, 0.01f);

            yield return new Modifier(ModifierType.Increase, EntityParameter.Evade, 0.01f);
        }
    }
}
