namespace Core.Interfaces.Entity
{
    using System;
    using System.Collections.Generic;

    public interface INpcModifiersComponent
    {
        IReadOnlyList<INpcModifier> AllModifiers { get; }
        void AddModifier(INpcModifier modifier);
        void RemoveModifier(string instanceId);
        void AddModifiers(List<INpcModifier> modifiers);
        event Action<INpcModifier>? ModifierAdded;
    }
}
