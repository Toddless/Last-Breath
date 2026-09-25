namespace Core.Save.Participants
{
    using Data.SaveData;
    using Newtonsoft.Json.Linq;
    using Reputation;

    public class PersonalReputationSaveParticipant(IPersonalReputationService personal) : ISaveParticipant
    {
        public string SectionId => "personalReputation";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.World;

        public JToken Capture()
        {
            var data = new PersonalReputationSaveData();
            foreach ((string instanceId, int points) in personal.Snapshot)
                data.Points[instanceId] = points;
            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<PersonalReputationSaveData>();
            if (saved == null) return;

            foreach ((string instanceId, int points) in saved.Points)
                personal.SetPersonal(instanceId, points);
        }
    }
}
