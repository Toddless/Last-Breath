namespace Core.Entity
{
    using System;
    using System.Collections.Generic;

    public interface INpcModifiersComponent
    {
        IReadOnlyList<INpcModifier> AllModifiers { get; }

        /// <summary>Fires for every ACCEPTED modifier — late scalers (ScaleModifier) scale newcomers through it.</summary>
        event Action<INpcModifier>? ModifierAdded;

        void AddModifier(INpcModifier modifier);
        void RemoveModifier(string instanceId);
        void AddModifiers(List<INpcModifier> modifiers);
    }
}
