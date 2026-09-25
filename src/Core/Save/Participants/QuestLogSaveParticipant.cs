namespace Core.Save.Participants
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Data.SaveData;
    using Narrative.Quests;
    using Newtonsoft.Json.Linq;

    /// <summary>Section "quests". v2 added the ledger of one-of-a-kind rewards already paid; a v1 file
    /// carries no ledger and restores as "nothing has been handed out yet". v3 names the current stage
    /// by id and remembers the ending the quest reached — branching makes a position meaningless; an
    /// older file is read by position and its counter snapshots are re-keyed to the stage id.</summary>
    public class QuestLogSaveParticipant(IQuestLogService questLog, IQuestProvider quests) : ISaveParticipant
    {
        public string SectionId => "quests";
        public int Version => 3;
        public int RestoreOrder => Save.RestoreOrder.Quests;

        public JToken Capture() => JToken.FromObject(new QuestLogSaveData
        {
            Quests = questLog.States.Select(state => new QuestStateSaveData
            {
                QuestId = state.QuestId,
                Status = state.Status.ToString(),
                StageId = state.StageId,
                OutcomeId = state.OutcomeId,
                CounterBaselines = new(state.CounterBaselines),
                GhostHintShown = state.GhostHintShown,
                AcceptedAtMinutes = state.AcceptedAtMinutes,
                NextOfferAtMinutes = state.NextOfferAtMinutes,
                GrantedUniqueRewards = [.. state.GrantedUniqueRewards],
            }).ToList()
        });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<QuestLogSaveData>();
            if (saved == null) return;

            questLog.RestoreState(saved.Quests
                .Where(entry => Enum.TryParse<QuestStatus>(entry.Status, out _))
                .Select(RestoreQuest));
        }

        private QuestState RestoreQuest(QuestStateSaveData entry)
        {
            string stageId = StageIdOf(entry);
            var state = new QuestState(entry.QuestId)
            {
                Status = Enum.Parse<QuestStatus>(entry.Status),
                StageId = stageId,
                OutcomeId = entry.OutcomeId,
                GhostHintShown = entry.GhostHintShown,
                AcceptedAtMinutes = entry.AcceptedAtMinutes,
                NextOfferAtMinutes = entry.NextOfferAtMinutes,
            };

            foreach ((string key, int baseline) in entry.CounterBaselines)
                state.CounterBaselines[Rekeyed(key, entry, stageId)] = baseline;
            foreach (string itemId in entry.GrantedUniqueRewards)
                state.GrantedUniqueRewards.Add(itemId);
            return state;
        }

        /// <summary>The stage the file names. A file written before stages were named gives a position,
        /// which is read off the quest's own stage list; a position the catalog no longer has restores
        /// the quest on its first stage.</summary>
        private string StageIdOf(QuestStateSaveData entry)
        {
            if (entry.StageId.Length > 0) return entry.StageId;
            if (quests.Get(entry.QuestId)?.Stages is not { Count: > 0 } stages) return string.Empty;

            int index = entry.StageIndex ?? 0;
            if (index >= 0 && index < stages.Count) return stages[index].Id;

            Tracker.TrackError($"Save puts quest '{entry.QuestId}' on stage #{index}, which it no longer has: it restarts on '{stages[0].Id}'");
            return stages[0].Id;
        }

        /// <summary>Counter snapshots of a pre-v3 file are keyed by stage position; the ones belonging to
        /// the restored stage take its id instead, so a half-finished "kill N new wolves" keeps its
        /// baseline. Snapshots of other stages are dead weight and stay as they are.</summary>
        private static string Rekeyed(string key, QuestStateSaveData entry, string stageId)
        {
            if (entry.StageId.Length > 0 || stageId.Length == 0) return key;

            string position = QuestState.BaselineKey((entry.StageIndex ?? 0).ToString(CultureInfo.InvariantCulture), string.Empty);
            return key.StartsWith(position, StringComparison.Ordinal)
                ? QuestState.BaselineKey(stageId, key[position.Length..])
                : key;
        }
    }
}
