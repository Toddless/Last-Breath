namespace Core.Interfaces.Abilities
{
    using System;

    public interface IAbilityUpgrade : IDisplayable, IIdentifiable
    {
        int Tier { get; }
        event Action? AbilityUpgradeChanged;
    }
}
