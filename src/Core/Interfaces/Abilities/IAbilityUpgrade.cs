namespace Core.Interfaces.Abilities
{
    using System;

    public interface IAbilityUpgrade : IIdentifiable, IDisplayable
    {
        int Tier { get; }
        int PointPerRank { get; }
        int MaxRank { get; }
        int CurrentRank { get; }

        event Action? AbilityUpgradeChanged;

        bool TryUpgradeRank(IAbility ability);
        void DowngradeRank(IAbility ability);
        void RemoveUpgrade(IAbility ability);
        void ApplyUpgrade(IAbility ability);

        IAbilityUpgrade Clone();
    }
}
