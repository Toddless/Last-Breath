namespace Battle.TestData.Abilities
{
    using Godot;
    using System;
    using Utilities;
    using Decorators;
    using System.Linq;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public class ReduceAbilityCostUpgrade : IAbilityUpgrade
    {
        private readonly List<AbilityParameterDecorator> _decorators = [];
        public string Id { get; }
        public string[] Tags { get; }
        public Texture2D? Icon { get; }
        public int Tier { get; }
        public int PointPerRank { get; }
        public int MaxRank { get; }
        public int CurrentRank { get; private set; }
        public float CostValue { get; }
        public float DamageValue { get; }
        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);

        public event Action? AbilityUpgradeChanged;

        public ReduceAbilityCostUpgrade(string id, string[] tags, Texture2D? icon, int tier, int pointPerRank, int maxRank, float costValue, float damageValue)
        {
            Id = id;
            Tags = tags;
            Icon = icon;
            Tier = tier;
            MaxRank = maxRank;
            CostValue = costValue;
            DamageValue = damageValue;
            PointPerRank = pointPerRank;
            _decorators.Add(new AbilityParameterChangeCostValueDecorator(() => CostValue * CurrentRank));
            _decorators.Add(new AbilityParameterChangeDamageDecorator(() => DamageValue * CurrentRank));
        }

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public bool TryUpgradeRank(IAbility ability)
        {
            if (CurrentRank == MaxRank) return false;
            float available = ability.AvailablePoints - PointPerRank;
            if (available < 0 || available < PointPerRank) return false;
            CurrentRank++;
            ability.AvailablePoints -= PointPerRank;
            AbilityUpgradeChanged?.Invoke();
            return true;
        }

        public void DowngradeRank(IAbility ability)
        {
            if (CurrentRank == 0) return;
            CurrentRank--;
            ability.AvailablePoints += PointPerRank;
            AbilityUpgradeChanged?.Invoke();
        }

        public void RemoveUpgrade(IAbility ability)
        {
            if (CurrentRank == 0) return;
            ability.AvailablePoints += PointPerRank * CurrentRank;
            CurrentRank = 0;

            foreach (AbilityParameterDecorator decorator in _decorators)
                ability.RemoveParameterUpgrade(decorator.Id, decorator.Parameter);
            AbilityUpgradeChanged?.Invoke();
        }

        public void ApplyUpgrade(IAbility ability)
        {
            foreach (AbilityParameterDecorator decorator in _decorators)
                ability.AddParameterUpgrade(decorator);
        }

        public IAbilityUpgrade Clone() => new ReduceAbilityCostUpgrade(Id, Tags, Icon, Tier, PointPerRank, MaxRank, CostValue, DamageValue);
    }
}
