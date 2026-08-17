namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using Data.AbilityData;
    using Data.GameData;
    using Newtonsoft.Json;

    /// <summary>
    /// What an ornament id means. Asked wherever one turns up — a quest reward, a bag, a save entry, an
    /// attachment — so it lives in the assembly every composition holds, beside the augment records and
    /// for the same reason: the battle module puts the socket on the board, this says what tier the
    /// socket takes.
    /// </summary>
    public interface IOrnamentCatalog
    {
        /// <summary>Every ornament the game declares. Small by design — the catalog is a handful of named
        /// artefacts — which is why a refusal can afford to list all of them.</summary>
        IReadOnlyCollection<OrnamentData> All { get; }

        /// <summary>The ornament's record, or null for an id no file declares. Null rather than a blank
        /// record: a record declaring tier 0 would read as an ornament granting a socket no augment
        /// fits, and that is a thing the game would then have to have rules about.</summary>
        OrnamentData? Find(string ornamentId);
    }

    /// <inheritdoc cref="IOrnamentCatalog"/>
    public class OrnamentCatalog : IOrnamentCatalog, IGameDataParticipant
    {
        private readonly Dictionary<string, OrnamentData> _ornaments = new(StringComparer.Ordinal);

        public IReadOnlyList<string> Catalogs => [DataCatalog.Ornaments];

        public IReadOnlyCollection<OrnamentData> All => _ornaments.Values;

        public OrnamentData? Find(string ornamentId) => _ornaments.GetValueOrDefault(ornamentId);

        public void Apply(string catalog, GameDataFile file)
        {
            var root = JsonConvert.DeserializeObject<OrnamentDataRoot>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize ornament data '{file.FileName}'");

            foreach (OrnamentData ornament in root.Ornaments)
            {
                if (ornament.Tier > 0)
                {
                    _ornaments[ornament.Id] = ornament;
                    continue;
                }

                // An ornament exists to grant a socket, and a socket of no tier is not one. Refused with
                // the id rather than defaulted, because the default would be an item the player is given
                // by a quest and can attach to nothing.
                Tracker.TrackError(
                    $"Ornament '{ornament.Id}' in '{file.FileName}' declares tier {ornament.Tier}: an ornament grants " +
                    "one socket of its own tier, so the record says nothing and is skipped.",
                    this);
            }
        }
    }
}
