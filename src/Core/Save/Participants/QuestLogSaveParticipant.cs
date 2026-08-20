namespace Core.Save.Participants
{
    using System;
    using System.Linq;
    using Data.SaveData;
    using Narrative.Quests;
    using Newtonsoft.Json.Linq;

    /// <summary>Section "quests". v2 added the ledger of one-of-a-kind rewards already paid; a v1 file
    /// carries no ledger and restores as "nothing has been handed out yet".</summary>
    public class QuestLogSaveParticipant(IQuestLogService questLog) : ISaveParticipant
    {
        public string SectionId => "quests";
        public int Version => 2;
        public int RestoreOrder => Save.RestoreOrder.Quests;

        public JToken Capture() => JToken.FromObject(new QuestLogSaveData
        {
            Quests = questLog.States.Select(state => new QuestStateSaveData
            {
                QuestId = state.QuestId,
                Status = state.Status.ToString(),
                StageIndex = state.StageIndex,
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
                .Select(entry =>
                {
                    var state = new QuestState(entry.QuestId)
                    {
                        Status = Enum.Parse<QuestStatus>(entry.Status),
                        StageIndex = entry.StageIndex,
                        GhostHintShown = entry.GhostHintShown,
                        AcceptedAtMinutes = entry.AcceptedAtMinutes,
                        NextOfferAtMinutes = entry.NextOfferAtMinutes,
                    };
                    foreach ((string key, int baseline) in entry.CounterBaselines)
                        state.CounterBaselines[key] = baseline;
                    foreach (string itemId in entry.GrantedUniqueRewards)
                        state.GrantedUniqueRewards.Add(itemId);
                    return state;
                }));
        }
    }
}
