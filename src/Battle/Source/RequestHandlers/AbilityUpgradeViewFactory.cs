namespace Battle.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Localization;
    using Core.Services;
    using Core.Views;
    using Godot;

    /// <summary>Builds the detail DTO from the learned ability instance in the player's book (no domain
    /// object leaves). What it reports as worn is read off the ability itself rather than off the
    /// board: the ability is what the numbers beside them were taken from, so the two cannot disagree
    /// in the same window.</summary>
    internal static class AbilityUpgradeViewFactory
    {
        public static AbilityUpgradeView Build(IPlayerAccessor playerAccessor, string abilityId)
        {
            var ability = playerAccessor.Player?.AbilityBook.AllAbilities.FirstOrDefault(a => a.Id == abilityId);
            if (ability == null)
                return new AbilityUpgradeView(abilityId, Localization.Localize(abilityId), string.Empty, string.Empty, string.Empty, []);

            var worn = ability.InstalledUpgrades
                .Select(installed => ToOption(installed.Key, installed.Value))
                .OrderBy(option => option.Tier)
                .ThenBy(option => option.SocketId, StringComparer.Ordinal)
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
                worn);
        }

        private static UpgradeOptionView ToOption(string socketId, IAbilityUpgrade upgrade) =>
            new(socketId, upgrade.DisplayName, upgrade.Description, upgrade.Tier);
    }
}
