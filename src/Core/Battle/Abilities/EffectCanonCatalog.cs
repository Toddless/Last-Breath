namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Data.EffectsData;
    using Data.GameData;
    using Newtonsoft.Json;

    /// <summary>
    /// Reads <c>SharedData/Effects</c>: the balanced figures, the strength and the stack ceiling of every
    /// effect. The rows are taken as written — nothing here knows what builds an effect, so a row naming
    /// an id no registry answers to is still read, and the registry names that pairing itself.
    /// </summary>
    public class EffectCanonCatalog : IEffectCanonCatalog, IGameDataParticipant
    {
        /// <summary>The key a stack ceiling is written under, the same name the effects are built by.</summary>
        private const string MaxStacksKey = "maxStacks";

        /// <summary>Canonical figures per effect id, exactly as the rows carry them.</summary>
        private readonly Dictionary<string, IReadOnlyDictionary<string, float>> _canon = new(StringComparer.Ordinal);

        /// <summary>Canonical strength per effect id; ids absent from here are weak, as most effects are.</summary>
        private readonly Dictionary<string, EffectPower> _powers = new(StringComparer.Ordinal);

        public IReadOnlyList<string> Catalogs => [DataCatalog.Effects];

        public IReadOnlyCollection<string> Ids => _canon.Keys;

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<EffectCatalogData>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize effect data '{file.FileName}'");

            foreach (EffectDefinitionData definition in data.Effects)
            {
                if (!TryReadPower(definition, out EffectPower power)) continue;

                // Named out loud, last declaration winning: the reader has no way of choosing between two
                // rows, and an author whose numbers silently collapse into one is left wondering which he plays.
                if (_canon.ContainsKey(definition.Id))
                    Tracker.TrackError($"Canonical row '{definition.Id}' is declared more than once ('{file.FileName}'); the last declaration wins");

                _canon[definition.Id] = new Dictionary<string, float>(definition.Properties, StringComparer.Ordinal);
                _powers[definition.Id] = power;
            }
        }

        public IReadOnlyDictionary<string, float>? CanonOf(string effectId) => _canon.GetValueOrDefault(effectId);

        public EffectPower PowerOf(string effectId) => _powers.GetValueOrDefault(effectId, EffectPower.Weak);

        public int? StackCeilingOf(string effectId) =>
            _canon.TryGetValue(effectId, out IReadOnlyDictionary<string, float>? canon)
            && canon.TryGetValue(MaxStacksKey, out float ceiling)
                ? (int)ceiling
                : null;

        /// <summary>The strength the row names, or Weak when it names none. A name that parses into nothing
        /// refuses the whole row: an unreadable strength must not pass for the default one.</summary>
        private static bool TryReadPower(EffectDefinitionData definition, out EffectPower power)
        {
            try
            {
                power = EnumParser.ParseEnumOrDefault<EffectPower>(definition.Power);
                return true;
            }
            catch (FormatException exception)
            {
                Tracker.TrackError($"Canonical row '{definition.Id}' not taken: {exception.Message}");
                power = EffectPower.Weak;
                return false;
            }
        }
    }
}
