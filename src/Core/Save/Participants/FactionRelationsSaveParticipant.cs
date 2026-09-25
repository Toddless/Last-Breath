namespace Core.Save.Participants
{
    using System;
    using System.Collections.Generic;
    using Data.SaveData;
    using Entity;
    using Enums;
    using Newtonsoft.Json.Linq;

    public class FactionRelationsSaveParticipant(IFactionRelationService relations) : ISaveParticipant
    {
        public string SectionId => "factionRelations";
        public int Version => 2;
        public int RestoreOrder => Save.RestoreOrder.World;

        public JToken Capture()
        {
            var data = new FactionRelationsSaveData();
            foreach (Fractions faction in Enum.GetValues<Fractions>())
            {
                data.PlayerReputation[faction.ToString()] = relations.GetReputation(faction);
                data.PlayerLevels[faction.ToString()] = relations.GetPlayerRelation(faction).ToString();
            }

            return JToken.FromObject(data);
        }

        public void Restore(JToken data, int savedVersion)
        {
            if (savedVersion < 2)
            {
                RestoreLegacyLevels(data);
                return;
            }

            var saved = data.ToObject<FactionRelationsSaveData>();
            if (saved == null) return;

            foreach ((string factionName, int points) in saved.PlayerReputation)
            {
                if (!Enum.TryParse(factionName, out Fractions faction)) continue; // faction removed from the game

                RelationLevel? level = null;
                if (saved.PlayerLevels.TryGetValue(factionName, out string? levelName) && Enum.TryParse(levelName, out RelationLevel parsed))
                    level = parsed;
                relations.SetReputation(faction, points, level);
            }
        }

        /// <summary>v1 stored only the level name — land the points at the middle of that level's band.</summary>
        private void RestoreLegacyLevels(JToken data)
        {
            var levelsByFaction = data["playerRelations"]?.ToObject<Dictionary<string, string>>();
            if (levelsByFaction == null) return;

            foreach ((string factionName, string levelName) in levelsByFaction)
            {
                if (!Enum.TryParse(factionName, out Fractions faction)) continue;
                if (!Enum.TryParse(levelName, out RelationLevel level)) continue;
                relations.SetPlayerRelation(faction, level);
            }
        }
    }
}
