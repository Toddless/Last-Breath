namespace Core.Save.Participants
{
    using System.Collections.Generic;
    using Narrative.Facts;
    using Newtonsoft.Json.Linq;

    public class WorldFactsSaveParticipant(IWorldFactsService facts) : ISaveParticipant
    {
        public string SectionId => "worldFacts";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.World;

        public JToken Capture() => JToken.FromObject(facts.Snapshot);

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<Dictionary<string, int>>();
            if (saved == null) return;
            facts.RestoreState(saved);
        }
    }
}
