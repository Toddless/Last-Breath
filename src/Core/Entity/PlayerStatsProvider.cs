namespace Core.Entity
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.GameData;
    using Data.PlayerStatsData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Reads the PlayerStats catalog — the player's baseline parameter values — so the character's
    /// starting point is content instead of a switch in the player scene. Both the game and the
    /// passive-tree tool measure against the very same numbers.
    /// </summary>
    public class PlayerStatsProvider : IPlayerStatsProvider, IGameDataParticipant
    {
        private static readonly Dictionary<EntityParameter, float> s_noProfile = [];

        private Dictionary<EntityParameter, float> _unarmed = s_noProfile;

        public IReadOnlyList<string> Catalogs => [DataCatalog.PlayerStats];

        public IReadOnlyDictionary<EntityParameter, float> Unarmed => _unarmed;

        /// <summary>Reads one profile, keeping the lines it understands. A misspelled parameter costs
        /// its own line and nothing else — a profile short one stat is still a better answer than no
        /// profile at all, and the skip is reported rather than swallowed.</summary>
        private static Dictionary<EntityParameter, float> Read(string name, Dictionary<string, float> values, string fileName)
        {
            Dictionary<EntityParameter, float> profile = [];

            foreach (KeyValuePair<string, float> pair in values)
            {
                if (!EnumParser.TryParseEnum(pair.Key, out EntityParameter parameter))
                {
                    Tracker.TrackError($"Skipping '{pair.Key}' of stat profile '{name}' in '{fileName}': not an {nameof(EntityParameter)}");
                    continue;
                }

                profile[parameter] = pair.Value;
            }

            return profile;
        }

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<PlayerStatsData>(file.Json)
                       ?? throw new InvalidOperationException($"'{file.FileName}' holds no stat profiles");

            _unarmed = Read(PlayerStatsData.UnarmedField, data.Unarmed, file.FileName);

            // Loud at load time, not on use: without the baseline every base stat answers zero, and a
            // player with zero health has to look like broken data instead of a strange balance pass.
            // A missing catalog is already reported by the load orchestrator (the source throws).
            if (_unarmed.Count == 0)
                Tracker.TrackError($"'{file.FileName}' leaves the {DataCatalog.PlayerStats} catalog without an '{PlayerStatsData.UnarmedField}' profile: every player base stat reads zero");
        }

        public float UnarmedValue(EntityParameter parameter) => Unarmed.GetValueOrDefault(parameter);
    }
}
