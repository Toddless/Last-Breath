namespace Core.Narrative
{
    using System.Collections.Generic;
    using Actions;
    using Conditions;

    /// <summary>
    /// Every condition and action the narrative vocabulary is written in, named without a running game.
    /// A factory needs live services to BUILD an entry; it needs none to say what an entry is written
    /// with, so the specs stand on their own and an authoring tool reads the words the parser will read
    /// back. The discriminator each entry carries is <see cref="NarrativeParameterSchema.TypeKey"/>.
    /// </summary>
    /// <remarks>Written out rather than reflected: a factory that stops being registered stops being part
    /// of the vocabulary, and reflection cannot tell the two apart. The order is the order the game
    /// registers the factories in; a test holds the lists against the registered set.</remarks>
    public static class NarrativeVocabulary
    {
        public static IReadOnlyList<NarrativeRecordSpec> Conditions { get; } =
        [
            HasItemConditionFactory.Spec,
            FactConditionFactory.Spec,
            FactionStandingConditionFactory.Spec,
            NpcRelationConditionFactory.Spec,
            AttributeConditionFactory.Spec,
            InfluenceConditionFactory.Spec,
            AllOfConditionFactory.Spec,
            AnyOfConditionFactory.Spec,
            NotConditionFactory.Spec,
            QuestStatusConditionFactory.Spec,
            CanAcceptQuestConditionFactory.Spec,
            CanTurnInQuestConditionFactory.Spec,
            QuestOfferRollConditionFactory.Spec,
        ];

        /// <summary>The quest kinds are named one by one: the registration loops over the enum, and a kind
        /// added there without a word here is a word the editor would never offer.</summary>
        public static IReadOnlyList<NarrativeRecordSpec> Actions { get; } =
        [
            SetFactActionFactory.Spec,
            GiveItemActionFactory.Spec,
            TakeItemActionFactory.Spec,
            PublishDeedActionFactory.Spec,
            AddReputationActionFactory.Spec,
            AddInfluenceExpActionFactory.Spec,
            StartTradeActionFactory.Spec,
            SpawnNpcActionFactory.Spec,
            GrantTreePointsActionFactory.Spec,
            QuestActionFactory.SpecFor(QuestActionKind.Accept),
            QuestActionFactory.SpecFor(QuestActionKind.Decline),
            QuestActionFactory.SpecFor(QuestActionKind.TurnIn),
            QuestActionFactory.SpecFor(QuestActionKind.Abandon),
        ];
    }
}
