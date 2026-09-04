namespace LastBreath.Descriptors
{
    using Core.Ai.World.Raids;
    using Core.Ai.World.Recovery;
    using Core.Battle;
    using Core.Data.CombatRulesData;
    using Core.Data.CraftingData;
    using Core.Data.FactionData;
    using Core.Data.GameData;
    using Core.Data.InfluenceData;
    using Core.Data.LootTable;
    using Core.Data.NpcSpawnRollsData;
    using Core.Data.PlayerStatsData;
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
        : SingleObjectDescriptor(DataCatalog.CombatRules, typeof(CombatRulesData), "CombatRules")
    {
        /// <summary>Json name of the control-resistance block — what
        /// <see cref="CombatRulesData.ControlResistance"/> is written as.</summary>
        public const string ControlResistanceField = "controlResistance";

        /// <summary>Json name of the statuses that block counts as hard control — what
        /// <see cref="ControlResistanceData.HardControlStatuses"/> is written as.</summary>
        public const string HardControlStatusesField = "hardControlStatuses";

        /// <summary>Json name of the kinds of foe it applies to — what
        /// <see cref="ControlResistanceData.AppliesTo"/> is written as.</summary>
        public const string AppliesToField = "appliesTo";

        /// <summary>Json name of the multicast block — what <see cref="CombatRulesData.Multicast"/> is
        /// written as.</summary>
        public const string MulticastField = "multicast";

        /// <summary>Json name of its ladder of stages — what <see cref="MulticastData.Stages"/> is
        /// written as.</summary>
        public const string StagesField = "stages";

        /// <summary>Json name of the share a stage rolls at — what
        /// <see cref="MulticastStageData.Chance"/> is written as.</summary>
        public const string ChanceField = "chance";

        /// <summary>Json name of the ceiling that share is held to — what
        /// <see cref="MulticastStageData.Cap"/> is written as.</summary>
        public const string CapField = "cap";
    }

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
    /// <remarks>Read off <see cref="RecoveryConfig"/>, which is both the record the file is parsed into
    /// and the shape the provider hands the game: one type, so a field cannot reach the tool without
    /// reaching the reader.</remarks>
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

    /// <summary>Who stands where: the directed matrix of faction against faction, the points scale the
    /// player's own standing is measured on, the traits that freeze a faction's standing or let it raid,
    /// and the standings a new game starts at. The folder is named for whose relations they are and the
    /// file for what it holds, which are two different facts.</summary>
    /// <remarks>One document and not a catalog of factions: the factions themselves are members of an
    /// enum, and every list in the file is keyed by one of them rather than by an id of its own.</remarks>
    public sealed class FactionsCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.Factions, typeof(FactionRelationsData), "FactionRelations")
    {
        /// <summary>Json name of the directed matrix — what <see cref="FactionRelationsData.Relations"/>
        /// is written as.</summary>
        public const string RelationsField = "relations";

        /// <summary>Json name of the faction one entry of the matrix speaks for — what
        /// <see cref="FactionRelationEntry.From"/> is written as.</summary>
        public const string FromField = "from";

        /// <summary>Json name of the faction it speaks about — what
        /// <see cref="FactionRelationEntry.To"/> is written as.</summary>
        public const string ToField = "to";

        /// <summary>Json name of the standing an entry states, on the matrix and on a threshold of the
        /// scale alike — what <see cref="FactionRelationEntry.Level"/> is written as.</summary>
        public const string LevelField = "level";

        /// <summary>Json name of the points scale — what <see cref="FactionRelationsData.Reputation"/> is
        /// written as.</summary>
        public const string ScaleField = "reputation";

        /// <summary>Json name of the thresholds on that scale — what
        /// <see cref="ReputationScaleData.Levels"/> is written as.</summary>
        public const string ThresholdsField = "levels";

        /// <summary>Json name of the faction traits — what <see cref="FactionRelationsData.Factions"/> is
        /// written as.</summary>
        public const string TraitsField = "factions";

        /// <summary>Json name of the standings a new game starts at — what
        /// <see cref="FactionRelationsData.PlayerDefaults"/> is written as.</summary>
        public const string DefaultsField = "playerDefaults";

        /// <summary>Json name of the faction a traits row and a starting standing are written for — what
        /// <see cref="FactionTraitsEntry.Fraction"/> is written as.</summary>
        public const string FractionField = "fraction";
    }

    /// <summary>How MANY of an npc's modifier and ability slots are actually filled: a chance ladder per
    /// kind of foe and the rarity ladder scaling all of them at once. WHICH modifier lands in a filled
    /// slot is the NpcModifiers catalog's answer, which is why this one is a folder of its own.</summary>
    /// <remarks>Read off the document the provider parses the file into, which is the only shape of it
    /// there is: nothing downstream sees the file, only the chances worked out from it.</remarks>
    public sealed class NpcSpawnRollsCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.NpcSpawnRolls, typeof(SpawnRollsData), "NpcSpawnRolls")
    {
        /// <summary>Json name of the map of foe kind to the ladder its modifier slots are rolled on — what
        /// <see cref="SpawnRollsData.Modifiers"/> is written as.</summary>
        public const string ModifiersField = "modifiers";

        /// <summary>Json name of the same map for the ability slots — what
        /// <see cref="SpawnRollsData.Abilities"/> is written as.</summary>
        public const string AbilitiesField = "abilities";

        /// <summary>Json name of the map of rarity to what it multiplies every chance by — what
        /// <see cref="SpawnRollsData.RarityMultipliers"/> is written as.</summary>
        public const string RarityMultipliersField = "rarityMultipliers";
    }

    /// <summary>The player's own baseline: what the character is worth with no weapon and no gear. One
    /// profile and not a catalog of them — the game reads the unarmed one and nothing reaches a second —
    /// so the document names it as a field of its own.</summary>
    public sealed class PlayerStatsCatalogDescriptor()
        : SingleObjectDescriptor(DataCatalog.PlayerStats, typeof(PlayerStatsData), "PlayerStats");

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
