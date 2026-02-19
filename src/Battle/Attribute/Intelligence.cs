namespace Battle.Attribute
{
    using Godot;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public class Intelligence(IModifiersComponent manager) :
        EntityAttribute(GetEffects(), manager,  new Modifier(ModifierType.Flat, EntityParameter.Intelligence, 0f))
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
            yield return new Modifier(ModifierType.Flat, EntityParameter.Barrier, 10);

            yield return new Modifier(ModifierType.Flat, EntityParameter.SpellDamage, 10f);

            yield return new Modifier(ModifierType.Flat, EntityParameter.ManaRecovery, 0.1f);

            yield return new Modifier(ModifierType.Flat, EntityParameter.Mana, 0.1f);
        }
    }
}
