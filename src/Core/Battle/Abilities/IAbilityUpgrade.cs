namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public interface IAbilityUpgrade : IDisplayable, IIdentifiable
    {
        int Tier { get; }
        bool Learned { get; }

        /// <summary>
        /// Named values for the description template; the provider fills it from the JSON
        /// upgradeProperties at creation, so placeholder = property name ({poisonDuration},
        /// fractions via {criticalChance:%}). Empty = static text.
        /// </summary>
        IReadOnlyDictionary<string, object?> DescriptionValues { get; set; }

        event Action? AbilityUpgradeChanged;

        /// <summary>
        /// Non-generic dispatch: the upgrade knows its concrete ability type and checks
        /// compatibility itself (implemented once in the AbilityUpgrade&lt;T&gt; base).
        /// Lets the base Ability select upgrades without knowing its own concrete type.
        /// </summary>
        void Apply(IAbility ability);

        void Remove(IAbility ability);

        /// <summary>Fresh unapplied instance with the same configuration — ability copies must not
        /// share upgrade objects (applied state would leak between owners).</summary>
        IAbilityUpgrade Copy();
    }
}
