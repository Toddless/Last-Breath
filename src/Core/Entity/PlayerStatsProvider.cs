namespace Core.Entity
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.GameData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Reads the PlayerStats catalog — a map of named stat profiles — so the character's baseline is
    /// content instead of a switch in the player scene. Both the game and the passive-tree tool
    /// measure against the very same numbers.
    /// </summary>
    public class PlayerStatsProvider : IPlayerStatsProvider, IGameDataParticipant
    {
        private const string UnarmedProfile = "unarmed";

        private static readonly Dictionary<EntityParameter, float> s_noProfile = [];

        private readonly Dictionary<string, Dictionary<EntityParameter, float>> _profiles = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> Catalogs => [DataCatalog.PlayerStats];

        public IReadOnlyDictionary<EntityParameter, float> Unarmed => _profiles.GetValueOrDefault(UnarmedProfile, s_noProfile);

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
            var profiles = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, float>>>(file.Json)
                           ?? throw new InvalidOperationException($"'{file.FileName}' is not a map of named stat profiles");

            foreach (KeyValuePair<string, Dictionary<string, float>> profile in profiles)
                _profiles[profile.Key] = Read(profile.Key, profile.Value, file.FileName);

            // Loud at load time, not on use: without the baseline every base stat answers zero, and a
            // player with zero health has to look like broken data instead of a strange balance pass.
            // A missing catalog is already reported by the load orchestrator (the source throws).
            if (!_profiles.ContainsKey(UnarmedProfile))
                Tracker.TrackError($"'{file.FileName}' leaves the {DataCatalog.PlayerStats} catalog without an '{UnarmedProfile}' profile: every player base stat reads zero");
        }

        public float UnarmedValue(EntityParameter parameter) => Unarmed.GetValueOrDefault(parameter);
    }
}
