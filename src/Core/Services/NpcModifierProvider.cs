namespace Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Data.GameData;
    using Data.NpcModifiersData;
    using Entity;
    using Entity.NpcModifiers;
    using Enums;

    /// <summary>
    /// Shared NPC modifier provider (used by LootGeneration and Battle): consumes the
    /// NpcModifiers catalog and serves copies. Parsing and factories live in Core.
    /// <para>Two catalogs and not one: how far "unique" reaches in a section is a rule about composing a
    /// spawn and stands with the other such rules in the spawn-rolls document, while the modifier catalog
    /// holds only the modifiers. The order the two are named in is the order they are read in, so the
    /// reach is known before the first modifier is built out of it.</para>
    /// </summary>
    public class NpcModifierProvider : INpcModifierProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, INpcModifier> _npcModifiers = [];
        private readonly INpcModifiersFactory _modifiersFactory = new NpcModifiersFactory();

        private IReadOnlyDictionary<string, NpcUniqueScope> _scopes = new Dictionary<string, NpcUniqueScope>();

        public IReadOnlyList<string> Catalogs => [DataCatalog.NpcSpawnRolls, DataCatalog.NpcModifiers];

        public void Apply(string catalog, GameDataFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            if (string.Equals(catalog, DataCatalog.NpcSpawnRolls, StringComparison.Ordinal))
            {
                _scopes = NpcModifiersParser.Scopes(file.Json);
                return;
            }

            ReadModifiers(file);
        }

        public INpcModifier GetModifier(string id) => !_npcModifiers.TryGetValue(id, out var modifier)
            ? throw new KeyNotFoundException($"No NPC modifier loaded for '{id}'")
            : modifier.Copy();

        /// <summary>Ordered like <see cref="GetAllModifiers"/> — the two describe one catalog, and a
        /// membership check reads the same either way.</summary>
        public List<string> GetAllModifierIds() => [.. _npcModifiers.Keys.OrderBy(id => id, StringComparer.Ordinal)];

        /// <summary>The rollable pool, ordered by id: callers turn this order into weight bands, so the
        /// order decides WHICH modifier a given roll picks. Unordered, that answer would follow the order
        /// the catalog happened to arrive in — which file of the folder was read first, and where inside
        /// it the entry sat — so moving an entry between files would silently re-aim every seeded draw.</summary>
        public IReadOnlyList<INpcModifier> GetAllModifiers() =>
            [.. _npcModifiers.Values.OrderBy(modifier => modifier.Id, StringComparer.Ordinal)];

        /// <summary>One file of the modifier catalog, section by section, added to what the folder has
        /// given so far: a catalog is a folder, and an id already served is kept.</summary>
        private void ReadModifiers(GameDataFile file)
        {
            var data = NpcModifiersParser.Parse(file.Json, _scopes);
            var modifiers = _modifiersFactory.CreateNpcModifiers(data);
            modifiers.ForEach(modifier => _npcModifiers.TryAdd(modifier.Id, modifier));
        }
    }
}
