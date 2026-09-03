namespace LastBreath.Descriptors
{
    using Core.Ai.World.Raids;
    using Core.Ai.World.Recovery;
    using Core.Battle;
    using Core.Data.CombatRulesData;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Core.Data.InfluenceData;
    using Core.Data.LootTable;
    using Core.Data.WorldData;
    using Core.PassiveTree.Rules;
    using Core.Trade;

    // The catalogs whose file is one settings document. Each states only what cannot be read off a
    // type: the folder it lives in, the DTO the game parses it into, and the name of its one file —
    // which is asked of the descriptor's own FileName. Everything else is SingleObjectDescriptor's,
    // said once for all of them.

    /// <summary>The rules a battle is fought under: control resistance, the arena's slots, exhaustion,
    /// the ceiling on effect extensions and the multicast ladder.</summary>
    public sealed class CombatRulesCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.CombatRules, typeof(CombatRulesData), "CombatRules");

    /// <summary>What a kill is worth and what its budget buys: tier prices and chances, the per-entity
    /// base budget and the rarity multipliers scaling it.</summary>
    public sealed class LootConfigurationCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.LootConfiguration, typeof(LootConfigurationData), "LootConfiguration")
    {
        /// <summary>Json name of the map of entity type to the budget a kill of it starts with — what
        /// <see cref="LootConfigurationData.BaseBudget"/> is written as.</summary>
        public const string BaseBudgetField = "baseBudget";

        /// <summary>Json name of the map of rarity to what it multiplies that budget by — what
        /// <see cref="LootConfigurationData.RarityMultipliers"/> is written as.</summary>
        public const string RarityMultipliersField = "rarityMultipliers";
    }

    /// <summary>What the passive tree COSTS to undo, as opposed to what it contains: the tree itself is
    /// a catalog of its own, whose reader would take a pricing document for a tree with no nodes.</summary>
    public sealed class PassiveTreeRulesCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.PassiveTreeRules, typeof(PassiveTreeRulesData), "PassiveTreeRules");

    /// <summary>The player's own death and rising: how long the body lies, what it gets back on its
    /// feet, and the burning that ends the wait. The folder is named for whose lifecycle it is and the
    /// file for what it holds.</summary>
    public sealed class PlayerLifecycleCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.Player, typeof(PlayerLifecycleData), "PlayerLifecycle");

    /// <summary>When a faction at Hatred comes for the player, how many of them come, and how long they
    /// look before they give up.</summary>
    public sealed class RaidsCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.Raids, typeof(RaidsData), "Raids");

    /// <summary>Resting: what a recovery zone gives back per game minute, and when an npc walks away
    /// from its routine to go and rest.</summary>
    /// <remarks>Read off <see cref="RecoveryConfig"/> — the shape the provider hands the game — because
    /// the record the file is deserialized into is private to that provider. The two are written field
    /// for field, and the shipped file is held against this schema by a test.</remarks>
    public sealed class RecoveryCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.Recovery, typeof(RecoveryConfig), "Recovery");

    /// <summary>The world's clock: how long a day is and where the parts of it begin. The folder is the
    /// world's and the file is the clock's, which are two different facts.</summary>
    public sealed class WorldClockCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.World, typeof(WorldClockData), "WorldClock");

    /// <summary>What an item is worth in gold: the multipliers turning an authored base price into the
    /// price of one instance, and the share a trader pays back for it.</summary>
    public sealed class TradeCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.Trade, typeof(TradeConfig), "TradeConfiguration")
    {
        /// <summary>Json name of the map of rarity to what it multiplies a price by — what
        /// <see cref="TradeConfig.RarityMultipliers"/> is written as.</summary>
        public const string RarityMultipliersField = "rarityMultipliers";
    }

    /// <summary>The influence curve: what a level costs and the two chances it moves.</summary>
    public sealed class InfluenceCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.Influence, typeof(InfluenceMasteryData), "InfluenceMastery");

    /// <summary>The martial art curve: the level cap, which is also the passive tree's budget.</summary>
    public sealed class MartialArtMasteryCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.MartialArtMastery, typeof(MartialArtMasteryData), "MartialArtMastery");

    /// <summary>The smith's curve: the experience a level costs, the six channels a level buys, and the
    /// gates and base chances the crafting operations read off it.</summary>
    public sealed class CraftingMasteryCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.CraftingMastery, typeof(CraftingMasteryData), "CraftingMastery")
    {
        /// <summary>Json name of the map of created-item rarity to its weight in the creation roll — what
        /// <see cref="CraftingMasteryData.RarityWeights"/> is written as.</summary>
        public const string RarityWeightsField = "rarityWeights";

        /// <summary>Json name of the map of rarity to the experience an operation on it pays — what
        /// <see cref="CraftingMasteryExpRewardsData.ByRarity"/> is written as.</summary>
        public const string ExpByRarityField = "byRarity";

        /// <summary>Json name of the map of operation to what it multiplies that experience by — what
        /// <see cref="CraftingMasteryExpRewardsData.ModeFactors"/> is written as.</summary>
        public const string ExpModeFactorsField = "modeFactors";

        /// <summary>Json name of the field holding the two maps above — what
        /// <see cref="CraftingMasteryData.ExpRewards"/> is written as.</summary>
        public const string ExpRewardsField = "expRewards";
    }
}
