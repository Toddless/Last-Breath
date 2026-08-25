namespace Core.Data.GameData
{
    /// <summary>
    /// Catalog names — the subfolders of a project's data root. A participant declares the
    /// catalogs it consumes by these names; only the data source knows the root path.
    /// </summary>
    public static class DataCatalog
    {
        public const string Abilities = "Abilities";
        public const string CombatRules = "CombatRules";
        public const string Npc = "Npc";
        public const string NpcBehaviors = "NpcBehaviors";
        public const string NpcBuffs = "NpcBuffs";
        public const string NpcModifiers = "NpcModifiers";
        public const string Factions = "Factions";
        public const string ReputationDeeds = "ReputationDeeds";
        public const string ReputationPerks = "ReputationPerks";
        public const string Raids = "Raids";
        public const string Influence = "Influence";
        public const string Quests = "Quests";
        public const string Dialogues = "Dialogues";
        public const string Formatting = "Formatting";
        public const string World = "World";
        public const string Recovery = "Recovery";
        public const string Player = "Player";
        public const string PlayerStats = "PlayerStats";
        public const string PassiveTree = "PassiveTree";

        /// <summary>Tuning of what the tree COSTS, kept out of the PassiveTree catalog on purpose: the
        /// reader of that one parses every file in it as a tree, so a second document there would be
        /// read as a tree with no nodes and take the allocation down with it.</summary>
        public const string PassiveTreeRules = "PassiveTreeRules";

        /// <summary>Which passives a tree node may name, and the fields each of them is tuned by. The
        /// registry that builds them is battle-side and out of the authoring tool's reach, so the tool
        /// reads this instead and a test keeps the two from drifting apart.</summary>
        public const string PassiveSkills = "PassiveSkills";
        public const string LootTables = "LootTables";
        public const string LootConfiguration = "LootConfiguration";
        public const string Items = "Items";
        public const string EquipItems = "EquipItems";
        public const string Recipes = "Recipes";
        public const string CraftingAdditives = "CraftingAdditives";
        public const string CraftingMastery = "CraftingMastery";
        public const string MartialArtMastery = "MartialArtMastery";
        public const string ItemEffects = "ItemEffects";

        /// <summary>The ornaments: which tier of socket each grants the ability wearing it. Kept out of
        /// the Abilities catalog, whose reader parses every file in it as an ability document.</summary>
        public const string Ornaments = "Ornaments";

        /// <summary>Canonical numbers and stack ceilings of the temporary effects — the one place their
        /// balance is written. Separate from ItemEffects, which says which effects an item may GRANT.</summary>
        public const string Effects = "Effects";
        public const string Resources = "Resources";
        public const string Trade = "Trade";
        public const string Traders = "Traders";
        public const string ModifierPools = "ModifierPools";
        public const string Conditions = "Conditions";
        public const string UpgradeCosts = "UpgradeCosts";
    }
}
