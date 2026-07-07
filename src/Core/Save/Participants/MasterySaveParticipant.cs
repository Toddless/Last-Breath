namespace Core.Save.Participants
{
    using Battle;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;

    public class MasterySaveParticipant(IMartialArtMastery mastery) : ISaveParticipant
    {
        public string SectionId => "mastery";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Mastery;

        public JToken Capture() => JToken.FromObject(new MasterySaveData
        {
            // CurrentLevel includes BonusLevel; only the earned base is persisted
            BaseLevel = mastery.CurrentLevel - mastery.BonusLevel,
            Experience = mastery.CurrentExperience
        });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<MasterySaveData>();
            if (saved == null) return;
            mastery.RestoreState(saved.BaseLevel, saved.Experience);
        }
    }
}
