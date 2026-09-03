namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Reputation;
    using Core.Services;

    /// <summary>
    /// The narrative vocabulary as the game's service provider registers it, built over whatever
    /// services the caller has: the running game's, a sandbox's, or a test's mocks. Nothing outside the
    /// game may register these itself — a second hand-written copy of the list is a language that drifts
    /// from the one the parsers actually speak.
    /// </summary>
    /// <remarks>Written out rather than reflected, for the reason <see cref="NarrativeVocabulary"/> is:
    /// a factory the game stopped registering is not part of the language any more, and reflection
    /// cannot tell the two apart. The ORDER is the order the game registers them in, and it is held
    /// against <see cref="NarrativeVocabulary"/> by a test.</remarks>
    public static class NarrativeFactories
    {
        public static List<INarrativeConditionFactory> Conditions(
            IInventory inventory,
            IWorldFactsService facts,
            IFactionRelationService relations,
            IPersonalReputationService personal,
            IPlayerAccessor player,
            IInfluenceMastery influence,
            IWorldClock clock,
            IRandomNumberGenerator rnd,
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
            new QuestOfferRollConditionFactory(facts, influence, clock, rnd, quests),
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
            IGameMessageBus messages,
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
            new StartTradeActionFactory(messages),
            new SpawnNpcActionFactory(npcs, npcModifiers, spawner, population, points),
            new GrantTreePointsActionFactory(mastery),
            .. Enum.GetValues<QuestActionKind>().Select(kind => new QuestActionFactory(log, kind)),
        ];
    }
}
