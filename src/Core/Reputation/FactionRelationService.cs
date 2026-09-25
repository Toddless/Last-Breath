namespace Core.Reputation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Data.FactionData;
    using Data.GameData;
    using Entity;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads FactionRelations.json and answers hostility questions. The faction matrix is DIRECTED
    /// and static: an entry "from → to" does not imply the reverse. The player's standing is integer
    /// points per faction: the level is derived from the JSON thresholds, switching only after the
    /// points beat a boundary by the hysteresis buffer; factions flagged without reputation ignore
    /// point changes entirely.
    /// </summary>
    public class FactionRelationService : IFactionRelationService, IGameDataParticipant, Session.ISessionResettable
    {
        private readonly Dictionary<(Fractions From, Fractions To), RelationLevel> _relations = [];
        private readonly Dictionary<Fractions, int> _reputation = [];
        private readonly Dictionary<Fractions, int> _defaults = [];
        private readonly Dictionary<Fractions, RelationLevel> _levels = [];
        private readonly Dictionary<Fractions, FactionTraitsEntry> _traits = [];
        private (RelationLevel Level, int From)[] _thresholds = [];
        private int _min;
        private int _max;
        private int _hysteresis;

        /// <summary>DI path: the container can't resolve FactionRelationsData — the data arrives later via Apply (GameDataService.LoadAll).</summary>
        public FactionRelationService()
        {
        }

        /// <summary>Test constructor: applies the data directly, no file access involved.</summary>
        public FactionRelationService(FactionRelationsData data) => ApplyData(data);

        public event Action<ReputationChangedArgs>? PlayerReputationChanged;

        public event Action<Fractions, RelationLevel>? PlayerRelationChanged;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Factions];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<FactionRelationsData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize faction relations");
            ApplyData(data);
        }

        public RelationLevel GetRelation(Fractions from, Fractions to) => from == to ? RelationLevel.Alliance : _relations.GetValueOrDefault((from, to), RelationLevel.Neutral);

        public bool IsHostile(Fractions from, Fractions to) => GetRelation(from, to) <= RelationLevel.Hostility;

        public int GetReputation(Fractions faction) => _reputation.GetValueOrDefault(faction);

        public void AddReputation(Fractions faction, int delta, string reason)
        {
            if (delta == 0 || !HasReputation(faction)) return;

            // The pre-change level anchors the hysteresis walk — resolve it before the points move.
            var levelBefore = GetPlayerRelation(faction);
            if (ChangePoints(faction, GetReputation(faction) + delta, reason))
                UpdateLevelWithHysteresis(faction, levelBefore);
        }

        public void SetReputation(Fractions faction, int points, RelationLevel? level = null)
        {
            var levelBefore = GetPlayerRelation(faction);
            ChangePoints(faction, points, ReputationReasons.DirectSet);
            SetLevel(faction, level ?? LevelFor(GetReputation(faction)), levelBefore);
        }

        public void SetPlayerRelation(Fractions faction, RelationLevel level) =>
            SetReputation(faction, MidpointOf(level), level);

        public RelationLevel GetPlayerRelation(Fractions faction) =>
            _levels.TryGetValue(faction, out var level) ? level : LevelFor(GetReputation(faction));

        public bool IsHostileToPlayer(Fractions faction) => GetPlayerRelation(faction) <= RelationLevel.Hostility;

        public bool HasReputation(Fractions faction) => !_traits.TryGetValue(faction, out var traits) || traits.HasReputation;

        public bool CanRaid(Fractions faction) => _traits.TryGetValue(faction, out var traits) && traits.CanRaid;

        private void ApplyData(FactionRelationsData data)
        {
            foreach (var entry in data.Relations)
                _relations[(EnumParser.ParseEnum<Fractions>(entry.From), EnumParser.ParseEnum<Fractions>(entry.To))] = EnumParser.ParseEnum<RelationLevel>(entry.Level);

            _min = data.Reputation.Min;
            _max = data.Reputation.Max;
            _hysteresis = data.Reputation.Hysteresis;
            _thresholds = data.Reputation.Levels
                .Select(entry => (Level: EnumParser.ParseEnum<RelationLevel>(entry.Level), entry.From))
                .OrderBy(threshold => threshold.From)
                .ToArray();
            if (_thresholds.Length == 0)
                throw new InvalidOperationException("FactionRelations.json: reputation.levels must define the level thresholds");

            foreach (var entry in data.Factions)
                _traits[EnumParser.ParseEnum<Fractions>(entry.Fraction)] = entry;

            _defaults.Clear();
            foreach (var entry in data.PlayerDefaults)
                _defaults[EnumParser.ParseEnum<Fractions>(entry.Fraction)] = Math.Clamp(entry.Points, _min, _max);
            foreach ((var faction, int points) in _defaults)
                _reputation[faction] = points;
        }

        /// <summary>Silent return of the player's standing to the data defaults; the static NPC matrix never changes.</summary>
        public void ResetSession()
        {
            _reputation.Clear();
            _levels.Clear();
            foreach ((var faction, int points) in _defaults)
                _reputation[faction] = points;
        }

        /// <summary>Clamps and stores the points; publishes and returns true only when they actually moved.</summary>
        private bool ChangePoints(Fractions faction, int points, string reason)
        {
            points = Math.Clamp(points, _min, _max);
            int previous = GetReputation(faction);
            if (points == previous) return false;

            _reputation[faction] = points;
            PlayerReputationChanged?.Invoke(new ReputationChangedArgs(faction, points, points - previous, reason));
            return true;
        }

        private void SetLevel(Fractions faction, RelationLevel level, RelationLevel previous)
        {
            _levels[faction] = level;
            if (previous != level) PlayerRelationChanged?.Invoke(faction, level);
        }

        /// <summary>Walks the level one boundary at a time from the pre-change level; each crossing must beat the threshold by the buffer.</summary>
        private void UpdateLevelWithHysteresis(Fractions faction, RelationLevel levelBefore)
        {
            if (_thresholds.Length == 0) return;

            int points = GetReputation(faction);
            int index = Array.FindIndex(_thresholds, threshold => threshold.Level == levelBefore);
            if (index < 0) index = LevelIndexFor(points);

            while (index + 1 < _thresholds.Length && points >= RiseBarrier(index + 1)) index++;
            while (index > 0 && points < _thresholds[index].From - _hysteresis) index--;
            SetLevel(faction, _thresholds[index].Level, levelBefore);
        }

        /// <summary>Capped at the scale maximum so the top level stays reachable.</summary>
        private int RiseBarrier(int index) => Math.Min(_thresholds[index].From + _hysteresis, _max);

        private RelationLevel LevelFor(int points) =>
            _thresholds.Length == 0 ? RelationLevel.Neutral : _thresholds[LevelIndexFor(points)].Level;

        private int LevelIndexFor(int points)
        {
            int index = 0;
            for (int i = 0; i < _thresholds.Length; i++)
                if (points >= _thresholds[i].From)
                    index = i;
            return index;
        }

        private int MidpointOf(RelationLevel level)
        {
            int index = Array.FindIndex(_thresholds, threshold => threshold.Level == level);
            if (index < 0) return 0;

            int upper = index + 1 < _thresholds.Length ? _thresholds[index + 1].From : _max;
            return (_thresholds[index].From + upper) / 2;
        }
    }
}
