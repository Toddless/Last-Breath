namespace LastBreath.Descriptors
{
    using System.Collections.Generic;
    using Core.Data.GameData;
    using Tooling.Schema;

    /// <summary>The one place that says which catalogs the authoring tool can be handed a schema for.
    /// The tool builds its editors from <see cref="All"/>; a test holds both lists against
    /// <see cref="DataCatalog"/>, so a catalog can be neither described twice nor forgotten.</summary>
    public static class CatalogDescriptors
    {
        public static IReadOnlyList<ICatalogDescriptor> All { get; } =
        [
            new AbilitiesCatalogDescriptor(),
            new NpcCatalogDescriptor(),
            new NpcBehaviorsCatalogDescriptor(),
            new LootTablesCatalogDescriptor(),
            new EquipItemsCatalogDescriptor(),
            new ModifierPoolsCatalogDescriptor(),
            new ResourcesCatalogDescriptor(),
            new DialoguesCatalogDescriptor(),
            new QuestsCatalogDescriptor(),
            new FormattingCatalogDescriptor(),

            // The crafting bench: what is made, what it is made of, what an operation costs and what a
            // finished item may be given on top.
            new RecipesCatalogDescriptor(),
            new UpgradeCostsCatalogDescriptor(),
            new CraftingAdditivesCatalogDescriptor(),
            new ItemEffectsCatalogDescriptor(),
            new OrnamentsCatalogDescriptor(),

            // Standing and trade: what a deed does to it, what a level of it buys, and the shops whose
            // prices it moves. The buffs stand here beside them because an npc modifier is what pulls one.
            new TradersCatalogDescriptor(),
            new ReputationDeedsCatalogDescriptor(),
            new ReputationPerksCatalogDescriptor(),
            new NpcBuffsCatalogDescriptor(),
            new NpcModifiersCatalogDescriptor(),

            // What a grant hands over and what a line is held up by: the three catalogs every other one
            // points INTO. Nothing here is granted by anything — they are where the behaviour is declared.
            new EffectsCatalogDescriptor(),
            new PassiveSkillsCatalogDescriptor(),
            new ConditionsCatalogDescriptor(),

            // The settings documents: one object each, and one shape between them.
            new CombatRulesCatalogDescriptor(),
            new LootConfigurationCatalogDescriptor(),
            new PassiveTreeRulesCatalogDescriptor(),
            new PlayerLifecycleCatalogDescriptor(),
            new RaidsCatalogDescriptor(),
            new RecoveryCatalogDescriptor(),
            new WorldClockCatalogDescriptor(),
            new TradeCatalogDescriptor(),
            new InfluenceCatalogDescriptor(),
            new MartialArtMasteryCatalogDescriptor(),
            new CraftingMasteryCatalogDescriptor(),
            new FactionsCatalogDescriptor(),
            new NpcSpawnRollsCatalogDescriptor(),
            new PlayerStatsCatalogDescriptor()
        ];

        /// <summary>Catalogs with no descriptor yet, named one by one rather than counted: a name added
        /// to <see cref="DataCatalog"/> has to be put on one of the two lists by hand.</summary>
        public static IReadOnlyList<string> NotYetDescribed { get; } =
        [
            // The tree itself, which is authored by a tool of its own outside this solution: a document of
            // nodes laid out on a canvas, whose records are read by that tool and by the game and by
            // nothing in between. A second editor built from a schema would be a second author of it.
            DataCatalog.PassiveTree,

            // The legacy plain items. A DTO reads the file — one array under "items" — and the shipped
            // file is written in another shape entirely, two sections of its own, so the parser finds no
            // records at all and every reference into the catalog answers to nothing. Its one record is
            // the coal a quest asks for and takes, which no other catalog declares and no kill drops, so
            // that quest cannot be finished. Describing the catalog would mean choosing between the shape
            // the game reads and the shape the file is in.
            DataCatalog.Items,
        ];
    }
}
