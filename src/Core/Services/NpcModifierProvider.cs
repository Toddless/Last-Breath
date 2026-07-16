namespace Core.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Data.GameData;
    using Data.NpcModifiersData;
    using Entity;
    using Entity.NpcModifiers;

    /// <summary>
    /// Shared NPC modifier provider (used by LootGeneration and Battle): consumes the
    /// NpcModifiers catalog and serves copies. Parsing and factories live in Core.
    /// </summary>
    public class NpcModifierProvider : INpcModifierProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, INpcModifier> _npcModifiers = [];
        private readonly INpcModifiersFactory _modifiersFactory = new NpcModifiersFactory();

        public IReadOnlyList<string> Catalogs => [DataCatalog.NpcModifiers];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = NpcModifiersParser.Parse(file.Json);
            var modifiers = _modifiersFactory.CreateNpcModifiers(data);
            modifiers.ForEach(modifier => _npcModifiers.TryAdd(modifier.Id, modifier));
        }

        public INpcModifier GetModifier(string id) => !_npcModifiers.TryGetValue(id, out var modifier)
            ? throw new KeyNotFoundException($"No NPC modifier loaded for '{id}'")
            : modifier.Copy();

        public List<string> GetAllModifierIds() => _npcModifiers.Keys.ToList();

        public IReadOnlyList<INpcModifier> GetAllModifiers() => _npcModifiers.Values.ToList();
    }
}
