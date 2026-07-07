namespace Core.Components
{
    using System;
    using System.Collections.Generic;
    using Battle.Abilities;
    using Enums;

    /// <summary>
    /// Single owner of every ability instance an entity has learned, partitioned by stance.
    /// Stances describe what CAN be learned (progression rules); the book owns what IS learned,
    /// equipped and currently visible. Cooldowns tick for all learned abilities regardless
    /// of the active stance, because the book sets the owner right at <see cref="Learn"/>.
    /// </summary>
    public interface IAbilityBookComponent
    {
        Stance CurrentStance { get; }

        /// <summary>Every learned ability across all stances (items, seals and "random ability" effects use this).</summary>
        IReadOnlyList<IAbility> AllAbilities { get; }

        /// <summary>Equipped abilities of the current stance, without empty slots.</summary>
        IReadOnlyList<IAbility> ActiveAbilities { get; }

        /// <summary>Slot layout of the current stance; null = empty slot. The HUD maps this 1:1 to its buttons.</summary>
        IReadOnlyList<IAbility?> ActiveSlots { get; }

        event Action<IAbility>? AbilityLearned;
        event Action<IAbility>? AbilityForgotten;
        event Action? ActiveAbilitiesChanged;

        IReadOnlyList<IAbility> GetAbilities(Stance stance);

        /// <summary>Slot layout of ANY stance (null = empty slot); the save system captures all stances.</summary>
        IReadOnlyList<IAbility?> GetSlotLayout(Stance stance);
        void Learn(Stance stance, IAbility ability);
        void Forget(string instanceId);
        void Equip(Stance stance, string instanceId, int slot);
        void Unequip(Stance stance, int slot);
        void SetStance(Stance stance);
    }
}
