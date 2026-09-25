namespace Core.Narrative.Conditions
{
    using System;
    using Ai.World.Time;
    using Entity.Components;
    using Facts;
    using Influence;
    using Newtonsoft.Json.Linq;
    using Quests;

    /// <summary>
    /// "Does the NPC bring the job up at all?" — an INVISIBLE Influence roll cached in the facts
    /// registry (so it survives saves): one outcome per cooldown window, re-rolled after it ends.
    /// IsMet deliberately writes the cache — the condition is a gate that remembers slamming shut.
    /// </summary>
    public class QuestOfferRollCondition(
        IWorldFactsService facts, IInfluenceMastery influence, IWorldClock clock, IRandomNumberGenerator rnd,
        Func<IQuestProvider> quests, string questId, int cooldownHours) : INarrativeCondition
    {
        private const int MinutesPerHour = 60;
        private const int MinutesPerDay = 1440;

        public bool IsMet(NarrativeContext context)
        {
            int now = clock.Day * MinutesPerDay + clock.MinuteOfDay;
            if (now < facts.GetCount(FactKeys.QuestOfferRollUntil(questId)))
                return facts.IsSet(FactKeys.QuestOfferRollPassed(questId));

            var quest = quests().Get(questId);
            if (quest == null) return false;

            bool passed = rnd.RandFloat() < influence.GetQuestOfferChance(quest.Tier);
            facts.SetCount(FactKeys.QuestOfferRollUntil(questId), now + cooldownHours * MinutesPerHour);
            facts.SetCount(FactKeys.QuestOfferRollPassed(questId), passed ? 1 : 0);
            return passed;
        }
    }

    public class QuestOfferRollConditionFactory(
        IWorldFactsService facts, IInfluenceMastery influence, IWorldClock clock, IRandomNumberGenerator rnd,
        Func<IQuestProvider> quests) : INarrativeConditionFactory
    {
        private const string TypeName = "QuestOfferRoll";
        private const string CooldownHoursKey = "cooldownHours";
        private const int DefaultCooldownHours = 24;

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            QuestIdParameter.Field,
            NarrativeParameterSchema.Integer(CooldownHoursKey, DefaultCooldownHours));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            string? questId = QuestIdParameter.Require(json, Type);
            if (questId == null) return null;

            return new QuestOfferRollCondition(facts, influence, clock, rnd, quests, questId,
                json.Value<int?>(CooldownHoursKey) ?? DefaultCooldownHours);
        }
    }
}
