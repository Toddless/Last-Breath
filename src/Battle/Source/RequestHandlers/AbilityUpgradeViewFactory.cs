namespace Battle.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Localization;
    using Core.Services;
    using Core.Views;
    using Godot;

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
                Localization.Render("UI_AbilityCost", new Dictionary<string, object?>
                {
                    ["Value"] = ability.CostValue,
                    ["Resource"] = Localization.Localize(ability.CostType.ToString()),
                }),
                Localization.Render("UI_AbilityCooldown", new Dictionary<string, object?> { ["Value"] = Mathf.RoundToInt(ability.Cooldown) }),
                ability.Description,
                options);
        }

        private static UpgradeOptionView ToOption(IAbilityUpgrade upgrade) =>
            new(upgrade.InstanceId, upgrade.DisplayName, upgrade.Description, upgrade.Tier, upgrade.Learned);
    }
}
