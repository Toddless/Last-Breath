namespace Core.Data.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameData;
    using Narrative;
    using Narrative.Actions;
    using Newtonsoft.Json.Linq;
    using QuestData;

    /// <summary>What the item catalogs of a run answer about an id a quest hands over. The game asks its
    /// providers and an authoring tool asks the documents it has open, and neither reading belongs in the
    /// rules — the two questions travel together because they come out of the same catalogs.</summary>
    public interface IQuestRewardItems
    {
        /// <summary>Whether any catalog of items declares the id at all — every one an item may be written
        /// out of: the equipment templates, the recipes, the ornaments and the resources. An id nothing
        /// declares cannot be judged one of a kind, so it must not pass by being unrecognised.</summary>
        bool Declares(string itemId);

        /// <summary>Whether the id names a thing the world holds exactly one of.</summary>
        bool IsUnique(string itemId);
    }

    /// <summary>An answer made of the two questions themselves, for a caller who has them but no type to
    /// hang them on.</summary>
    public sealed class QuestRewardItems(Func<string, bool> declares, Func<string, bool> unique) : IQuestRewardItems
    {
        public bool Declares(string itemId) => declares(itemId);

        public bool IsUnique(string itemId) => unique(itemId);
    }

    /// <summary>
    /// What a quest puts in the player's hands, held against the catalogs the item comes out of. A quest
    /// handing out a one-of-a-kind item must not be repeatable, or every turn-in would mint another copy
    /// of an artefact the world was written to hold one of — and an id no catalog declares is one the
    /// mint has nothing to build and this rule cannot judge at all.
    /// <para>Every door counts: the quest-wide reward list, the reward list of every ending it can come
    /// to, and every GiveItem action it runs — on accepting, on declining, on failing, and on entering or
    /// finishing any stage. A guard reading only the reward list is one a hand-out walks past.</para>
    /// </summary>
    public static class QuestRewardRules
    {
        private const string RepeatedFormat =
            "the quest is written repeatable and hands over '{0}', which the world holds exactly one of: "
            + "every turn-in mints another copy of it.";

        private const string UndeclaredFormat =
            "'{0}' is handed over here and no catalog of items declares it: the mint has nothing to build, "
            + "and whether the reward is one of a kind cannot be judged at all.";

        /// <summary>What every hand-out of every quest comes to, in the order the quests are written and
        /// each id said once per quest. A quest naming its artefact in two places owes one fix.</summary>
        public static IReadOnlyList<DataFinding> Check(IReadOnlyList<QuestEntry> quests, IQuestRewardItems items)
        {
            ArgumentNullException.ThrowIfNull(quests);
            ArgumentNullException.ThrowIfNull(items);

            List<DataFinding> found = [];

            foreach (QuestEntry quest in quests)
                foreach (string itemId in HandedOver(quest).Distinct(StringComparer.Ordinal))
                    Judge(quest, itemId, items, found);

            return found;
        }

        /// <summary>One hand-out. Both rules are asked of it: an id nothing declares is reported as that
        /// and is not silently also called common, since the uniqueness answer about it means nothing.</summary>
        private static void Judge(QuestEntry quest, string itemId, IQuestRewardItems items, ICollection<DataFinding> found)
        {
            if (!items.Declares(itemId))
            {
                found.Add(Handed(DataFindingKind.UndeclaredReward, quest, itemId, string.Format(UndeclaredFormat, itemId)));
                return;
            }

            if (!quest.Repeatable || !items.IsUnique(itemId)) return;

            found.Add(Handed(DataFindingKind.RepeatedUniqueReward, quest, itemId, string.Format(RepeatedFormat, itemId)));
        }

        private static DataFinding Handed(DataFindingKind kind, QuestEntry quest, string itemId, string message) =>
            new(kind, DataCatalog.Quests, quest.Id, itemId, Where: string.Empty, message);

        /// <summary>Every id a quest puts in the player's hands: the reward lists and the actions alike.</summary>
        private static IEnumerable<string> HandedOver(QuestEntry quest) =>
            Rewards(quest).SelectMany(rewards => rewards.Items ?? []).Select(reward => reward.ItemId).Concat(Given(quest));

        /// <summary>Every reward list of a quest: its own and the one of each ending it can come to. A list
        /// written as null in the file is one json takes the word for and hands the reader nothing —
        /// a record still being typed, which is nothing handed over rather than a run that stops.</summary>
        private static IEnumerable<QuestRewardsEntry> Rewards(QuestEntry quest)
        {
            QuestRewardsEntry?[] written = [quest.Rewards, .. Stages(quest).Select(stage => stage.Outcome?.Rewards)];

            return written.OfType<QuestRewardsEntry>();
        }

        /// <summary>The stages a quest writes; none where the file wrote the list as null.</summary>
        private static IReadOnlyList<QuestStageEntry> Stages(QuestEntry quest) => quest.Stages ?? [];

        /// <summary>The ids handed over by an action rather than by a list. Read out of the raw entries:
        /// the actions are built by a vocabulary that needs a running world, and what an entry NAMES is
        /// written in the entry itself.</summary>
        private static IEnumerable<string> Given(QuestEntry quest) =>
            Actions(quest)
                .SelectMany(actions => actions ?? new JArray())
                .OfType<JObject>()
                .Where(action => string.Equals(
                    (string?)action[NarrativeParameterSchema.TypeKey], GiveItemActionFactory.Spec.TypeName, StringComparison.Ordinal))
                .Select(action => (string?)action[ItemReference.Key] ?? string.Empty);

        private static IEnumerable<JToken?> Actions(QuestEntry quest) =>
        [
            .. Rewards(quest).Select(rewards => rewards.Actions),
            quest.OnAccept,
            quest.OnDecline,
            quest.OnFail,
            .. Stages(quest).SelectMany(stage => new[] { stage.OnEnter, stage.OnComplete }),
        ];
    }
}
