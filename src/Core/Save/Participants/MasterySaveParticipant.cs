namespace Core.Save.Participants
{
    using Battle;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;

    public class MasterySaveParticipant(IMartialArtMastery mastery) : ISaveParticipant
    {
        public string SectionId => "mastery";

        /// <summary>2 — the granted tree points joined the section. A version 1 file simply carries
        /// none, which is what a save written before quests paid in points means.</summary>
        public int Version => 2;

        public int RestoreOrder => Save.RestoreOrder.Mastery;

        public JToken Capture() => JToken.FromObject(new MasterySaveData
        {
            // CurrentLevel includes BonusLevel; only the earned base is persisted
            BaseLevel = mastery.CurrentLevel - mastery.BonusLevel,
            Experience = mastery.CurrentExperience,
            // The tree persists no budget of its own — the whole granted total is restated from here,
            // which is why this section restores before the allocation that spends it.
            BonusPoints = mastery.BonusPoints
        });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<MasterySaveData>();
            if (saved == null) return;
            mastery.RestoreState(saved.BaseLevel, saved.Experience, saved.BonusPoints);
        }
    }
}
