namespace Core.Interfaces.Components
{
    using Enums;
    using System;
    using Modifiers;
    using System.Collections.Generic;

    public interface IParameterModifiersComponent
    {
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> EntityModifiers { get; }

        event EventHandler<IModifiersChangedEventArgs>? ModifiersChanged;

        IReadOnlyList<IModifierInstance> GetModifiers(EntityParameter parameter);

        void AddModifier(IModifierInstance modifier);
        void RemoveModifier(IModifierInstance modifier);
        void RemoveModifierBySource(object source);
        void UpdateModifier(IModifierInstance newModifier);
        void UpdateModifiers(IEnumerable<IModifierInstance> modifiers);
    }
}
