namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Time;
    using Core.Entity;
    using Core.Inventory;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Reputation;
    using Core.Services;

    /// <summary>
    /// The condition vocabulary as GameServiceProvider registers it, in one place: the tests cannot
    /// reference the game project, and two hand-written copies of this list would drift apart.
    /// The reflection pin in <see cref="NarrativeConditionSchemaTests"/> is what keeps it level with Core.
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
    }
}
