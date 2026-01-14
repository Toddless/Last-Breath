namespace Battle.Source.Abilities.Upgrades
{
    using System;
    using Core.Interfaces.Abilities;
    using Godot;
    using Utilities;

    public abstract class AbilityUpgrade(string id, string[] tags,  int tier, int pointPerRank, int maxRank, int currentRank = 0) : IAbilityUpgrade
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();


        public string[] Tags { get; } = tags;

        public Texture2D? Icon { get; }
        public int Tier { get; } = tier;
        public int PointPerRank { get; } = pointPerRank;
        public int MaxRank { get; } = maxRank;
        public int CurrentRank { get; protected set; } = currentRank;
        public string Description => Localization.LocalizeDescription(Id);
        public string DisplayName => Localization.Localize(Id);

        public event Action? AbilityUpgradeChanged;

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public virtual bool TryUpgradeRank(IAbility ability)
        {
            if (CurrentRank == MaxRank) return false;
            float available = ability.SpendAbilityPoints - PointPerRank;
            if (available < 0 || available < PointPerRank) return false;
            CurrentRank++;
            ability.SpendAbilityPoints -= PointPerRank;
            AbilityUpgradeChanged?.Invoke();
            return true;
        }

        public virtual void DowngradeRank(IAbility ability)
        {
            if (CurrentRank == 0) return;
            CurrentRank--;
            ability.SpendAbilityPoints += PointPerRank;
            AbilityUpgradeChanged?.Invoke();
        }

        public virtual void RemoveUpgrade(IAbility ability)
        {
            if (CurrentRank == 0) return;
            ability.SpendAbilityPoints += PointPerRank * CurrentRank;
            CurrentRank = 0;
            AbilityUpgradeChanged?.Invoke();
        }

        public virtual void ApplyUpgrade(IAbility ability)
        {
            AbilityUpgradeChanged?.Invoke();
        }

        public abstract IAbilityUpgrade Clone();
    }
}
