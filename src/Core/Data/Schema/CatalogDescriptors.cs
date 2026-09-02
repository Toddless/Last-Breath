namespace Core.Data.Schema
{
    using System.Collections.Generic;
    using GameData;
    using Tooling.Schema;

    /// <summary>The one place that says which catalogs the authoring tool can be handed a schema for.
    /// The tool builds its editors from <see cref="All"/>; a test holds both lists against
    /// <see cref="DataCatalog"/>, so a catalog can be neither described twice nor forgotten.</summary>
    public static class CatalogDescriptors
    {
        public static IReadOnlyList<ICatalogDescriptor> All { get; } =
            [new NpcCatalogDescriptor(), new LootTablesCatalogDescriptor(), new EquipItemsCatalogDescriptor()];

        /// <summary>Catalogs with no descriptor yet, named one by one rather than counted: a name added
        /// to <see cref="DataCatalog"/> has to be put on one of the two lists by hand.</summary>
        public static IReadOnlyList<string> NotYetDescribed { get; } =
        [
            DataCatalog.Abilities,
            DataCatalog.CombatRules,
            DataCatalog.NpcBehaviors,
            DataCatalog.NpcBuffs,
            DataCatalog.NpcModifiers,
            DataCatalog.NpcSpawnRolls,
            DataCatalog.Factions,
            DataCatalog.ReputationDeeds,
            DataCatalog.ReputationPerks,
            DataCatalog.Raids,
            DataCatalog.Influence,
            DataCatalog.Quests,
            DataCatalog.Dialogues,
            DataCatalog.Formatting,
            DataCatalog.World,
            DataCatalog.Recovery,
            DataCatalog.Player,
            DataCatalog.PlayerStats,
            DataCatalog.PassiveTree,
            DataCatalog.PassiveTreeRules,
            DataCatalog.PassiveSkills,
            DataCatalog.LootConfiguration,
            DataCatalog.Items,
            DataCatalog.Recipes,
            DataCatalog.CraftingAdditives,
            DataCatalog.CraftingMastery,
            DataCatalog.MartialArtMastery,
            DataCatalog.ItemEffects,
            DataCatalog.Ornaments,
            DataCatalog.Effects,
            DataCatalog.Resources,
            DataCatalog.Trade,
            DataCatalog.Traders,
            DataCatalog.ModifierPools,
            DataCatalog.Conditions,
            DataCatalog.UpgradeCosts,
        ];
    }
}
