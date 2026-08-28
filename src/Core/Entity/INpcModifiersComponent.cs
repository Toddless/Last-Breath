namespace Core.Entity
{
    using System;
    using System.Collections.Generic;
    using NpcModifiers;

    public interface INpcModifiersComponent
    {
        IReadOnlyList<INpcModifier> AllModifiers { get; }

        /// <summary>Fires for every ACCEPTED modifier — late scalers (ScaleModifier) scale newcomers through it.</summary>
        event Action<INpcModifier>? ModifierAdded;

        /// <summary>Wires the bearer side of the list (NpcBuffId → parameters and granted passives).
        /// Install it BEFORE the modifiers arrive so the batch binds in one settled pass.</summary>
        void UseBuffs(INpcBuffBinder binder);

        void AddModifier(INpcModifier modifier);
        void RemoveModifier(string instanceId);
        void AddModifiers(List<INpcModifier> modifiers);
    }
}
