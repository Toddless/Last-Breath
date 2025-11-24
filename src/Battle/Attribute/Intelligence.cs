namespace Battle.Attribute
{
    using Godot;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public class Intelligence(IModifierManager manager) : EntityAttribute(GetEffects(), manager,  new Modifier(ModifierType.Flat, EntityParameter.Intelligence, 1f))
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

            yield return new Modifier(ModifierType.Flat, EntityParameter.SpellDamage, 0.2f);

            yield return new Modifier(ModifierType.Flat, EntityParameter.ResourceRecovery, 0.1f);
        }
    }
}
