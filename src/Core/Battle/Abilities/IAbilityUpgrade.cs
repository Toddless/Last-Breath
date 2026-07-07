namespace Core.Battle.Abilities
{
    using System;
    using Interfaces;

    public interface IAbilityUpgrade : IDisplayable, IIdentifiable
    {
        int Tier { get; }
        bool Learned { get; }
        event Action? AbilityUpgradeChanged;

        /// <summary>
        /// Non-generic dispatch: the upgrade knows its concrete ability type and checks
        /// compatibility itself (implemented once in the AbilityUpgrade&lt;T&gt; base).
        /// Lets the base Ability select upgrades without knowing its own concrete type.
        /// </summary>
        void Apply(IAbility ability);

        void Remove(IAbility ability);
    }
}
