namespace Battle.Source.RequestHandlers
{
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Views;
    using Godot;
    using Utilities;

    /// <summary>Builds the detail DTO from the learned ability instance in the player's book (no domain object leaves).</summary>
    internal static class AbilityUpgradeViewFactory
    {
        public static AbilityUpgradeView Build(IPlayerAccessor playerAccessor, string abilityId)
        {
            var ability = playerAccessor.Player?.AbilityBook.AllAbilities.FirstOrDefault(a => a.Id == abilityId);
            if (ability == null)
                return new AbilityUpgradeView(abilityId, Localization.Localize(abilityId), string.Empty, string.Empty, string.Empty, []);

            var options = ability.Upgrades
                .OrderBy(tier => tier.Key)
                .SelectMany(tier => tier.Value.Select(ToOption))
                .ToList();

            return new AbilityUpgradeView(
                abilityId,
                ability.DisplayName,
                $"{ability.CostValue} {ability.CostType}",
                Mathf.RoundToInt(ability.Cooldown).ToString(),
                ability.Description,
                options);
        }

        private static UpgradeOptionView ToOption(IAbilityUpgrade upgrade) =>
            new(upgrade.InstanceId, upgrade.DisplayName, upgrade.Description, upgrade.Tier, upgrade.Learned);
    }
}
