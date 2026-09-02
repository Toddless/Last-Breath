namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Reputation;
    using Core.Services;

    /// <summary>
    /// The narrative vocabulary as GameServiceProvider registers it, in one place: the tests cannot
    /// reference the game project, and two hand-written copies of these lists would drift apart.
    /// The reflection pins in <see cref="NarrativeConditionSchemaTests"/> and
    /// <see cref="NarrativeActionSchemaTests"/> are what keep them level with Core.
    /// </summary>
    internal static class NarrativeTestFactories
    {
        /// <summary>Godot's RandomNumberGenerator cannot exist outside the engine, so the offer roll is
        /// handed none; nothing but its own IsMet ever reaches for it.</summary>
        public static List<INarrativeConditionFactory> Conditions(
            IInventory inventory,
            IWorldFactsService facts,
            IFactionRelationService relations,
            IPersonalReputationService personal,
            IPlayerAccessor player,
            IInfluenceMastery influence,
            IWorldClock clock,
            Func<IQuestLogService> log,
            Func<IQuestProvider> quests) =>
        [
            new HasItemConditionFactory(inventory),
            new FactConditionFactory(facts),
            new FactionStandingConditionFactory(relations),
            new NpcRelationConditionFactory(personal),
            new AttributeConditionFactory(player),
            new InfluenceConditionFactory(influence),
            new AllOfConditionFactory(),
            new AnyOfConditionFactory(),
            new NotConditionFactory(),
            new QuestStatusConditionFactory(log),
            new CanAcceptQuestConditionFactory(log),
            new CanTurnInQuestConditionFactory(log),
            new QuestOfferRollConditionFactory(facts, influence, clock, rnd: null!, quests),
        ];

        /// <summary>One factory per quest kind, exactly as the registration loop builds them.</summary>
        public static List<INarrativeActionFactory> Actions(
            IWorldFactsService facts,
            IInventory inventory,
            IItemMinter items,
            IGameEventBus events,
            IPlayerAccessor player,
            IFactionRelationService relations,
            IInfluenceMastery influence,
            IMartialArtMastery mastery,
            IGameMessageBus messageBus,
            INpcProvider npcs,
            INpcModifierProvider npcModifiers,
            INpcWorldSpawner spawner,
            INpcPopulationService population,
            ISpawnPointRegistry points,
            Func<IQuestLogService> log) =>
        [
            new SetFactActionFactory(facts),
            new GiveItemActionFactory(items, inventory),
            new TakeItemActionFactory(inventory),
            new PublishDeedActionFactory(events, player),
            new AddReputationActionFactory(relations),
            new AddInfluenceExpActionFactory(influence),
            new GrantTreePointsActionFactory(mastery),
            new StartTradeActionFactory(messageBus),
            new SpawnNpcActionFactory(npcs, npcModifiers, spawner, population, points),
            .. Enum.GetValues<QuestActionKind>().Select(kind => new QuestActionFactory(log, kind)),
        ];
    }
}
