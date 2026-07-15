namespace LastBreath.Attribute
{
    using System.Collections.Generic;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Godot;

    public class Intelligence(IParameterModifiersComponent manager) :
        EntityAttribute(GetEffects(), manager,  new Modifier(ModifierValueType.Flat, EntityParameter.Intelligence, 0f))
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
            yield return new Modifier(ModifierValueType.Flat, EntityParameter.Barrier, 10);

            yield return new Modifier(ModifierValueType.Flat, EntityParameter.SpellDamage, 10f);

            yield return new Modifier(ModifierValueType.Flat, EntityParameter.ManaRecovery, 0.1f);

            yield return new Modifier(ModifierValueType.Flat, EntityParameter.Mana, 0.1f);
        }
    }
}
