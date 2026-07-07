namespace Core.Save.Participants
{
    using System;
    using Data.SaveData;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;

    public class FactionRelationsSaveParticipant(IFactionRelationService relations) : ISaveParticipant
    {
        public string SectionId => "factionRelations";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.World;

        public JToken Capture()
        {
            var data = new FactionRelationsSaveData();
            foreach (Fractions faction in Enum.GetValues<Fractions>())
                data.PlayerRelations[faction.ToString()] = relations.GetPlayerRelation(faction).ToString();
            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<FactionRelationsSaveData>();
            if (saved == null) return;

            foreach ((string factionName, string levelName) in saved.PlayerRelations)
            {
                if (!Enum.TryParse(factionName, out Fractions faction)) continue; // faction removed from the game
                if (!Enum.TryParse(levelName, out RelationLevel level)) continue;
                relations.SetPlayerRelation(faction, level);
            }
        }
    }
}
