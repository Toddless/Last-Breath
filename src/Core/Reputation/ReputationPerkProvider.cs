namespace Core.Reputation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Data.GameData;
    using Data.ReputationData;
    using Entity;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>A single standing bonus: an id future consumers key off (prices, rewards, access) and its magnitude.</summary>
    public record ReputationPerk(string Id, float Value);

    /// <summary>
    /// Foundation only: maps standing levels to their perk sets from ReputationPerks.json.
    /// Consumers (trade prices, quest rewards, settlement access) plug in when those systems
    /// exist — nothing reads the perks yet.
    /// </summary>
    public interface IReputationPerkProvider
    {
        /// <summary>Perks granted by the player's CURRENT standing with the faction; empty when the level grants none.</summary>
        IReadOnlyList<ReputationPerk> GetPerks(Fractions faction);

        IReadOnlyList<ReputationPerk> GetPerksForLevel(RelationLevel level);
    }

    public class ReputationPerkProvider(IFactionRelationService relations) : IReputationPerkProvider, IGameDataParticipant
    {
        private readonly Dictionary<RelationLevel, List<ReputationPerk>> _perks = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.ReputationPerks];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<ReputationPerksData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize reputation perks");

            foreach (var entry in data.Levels)
                _perks[EnumParser.ParseEnum<RelationLevel>(entry.Level)] =
                    entry.Perks.Select(perk => new ReputationPerk(perk.Id, perk.Value)).ToList();
        }

        public IReadOnlyList<ReputationPerk> GetPerks(Fractions faction) =>
            GetPerksForLevel(relations.GetPlayerRelation(faction));

        public IReadOnlyList<ReputationPerk> GetPerksForLevel(RelationLevel level) =>
            _perks.TryGetValue(level, out var perks) ? perks : [];
    }
}
