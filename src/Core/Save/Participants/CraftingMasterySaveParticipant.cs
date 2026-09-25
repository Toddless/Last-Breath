namespace Core.Save.Participants
{
    using Crafting;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;

    public class CraftingMasterySaveParticipant(ICraftingMastery mastery) : ISaveParticipant
    {
        public string SectionId => "craftingMastery";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Mastery;

        public JToken Capture() => JToken.FromObject(new MasterySaveData
        {
            // Unlike the martial mastery, CurrentLevel here is already the bare base:
            // BonusLevel is a separate field the channels add on top.
            BaseLevel = mastery.CurrentLevel,
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
