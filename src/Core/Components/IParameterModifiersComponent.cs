namespace Core.Components
{
    using System;
    using System.Collections.Generic;
    using Enums;
    using Modifiers;

    public interface IParameterModifiersComponent
    {
        IReadOnlyDictionary<EntityParameter, List<IModifierInstance>> EntityModifiers { get; }

        event EventHandler<IModifiersChangedEventArgs>? ModifiersChanged;

        IReadOnlyList<IModifierInstance> GetModifiers(EntityParameter parameter);

        void AddModifier(IModifierInstance modifier);
        void RemoveModifier(IModifierInstance modifier);
        void RemoveModifierBySource(string source);
        void UpdateModifier(IModifierInstance newModifier);
        void UpdateModifiers(IEnumerable<IModifierInstance> modifiers);
        void RegisterSource(IParameterModifierSource source);
        void UnregisterSource(IParameterModifierSource source);
    }
}
