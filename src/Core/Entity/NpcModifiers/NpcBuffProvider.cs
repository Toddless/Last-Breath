namespace Core.Entity.NpcModifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Data.GameData;
    using Data.NpcBuffsData;
    using Entity;
    using Enums;
    using Modifiers;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads NpcBuffs.json and turns NpcBuffId into parameter modifiers. This is the "some NPCs
    /// are unbeatable with default skills" lever: a modifier attaching to an NPC pulls its buff.
    /// </summary>
    public class NpcBuffProvider : INpcBuffProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, NpcBuffData> _buffs = [];

        public NpcBuffProvider()
        {
        }

        /// <summary>Test constructor: applies the data directly, no file access involved.</summary>
        public NpcBuffProvider(NpcBuffsData data) => ApplyData(data);

        public IReadOnlyList<string> Catalogs => [DataCatalog.NpcBuffs];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<NpcBuffsData>(file.Json)
                       ?? throw new InvalidOperationException("Failed to deserialize NPC buffs");
            ApplyData(data);
        }

        public bool HasBuff(string buffId) => _buffs.ContainsKey(buffId);

        public IReadOnlyList<NpcBuffGrantData> GetGrants(string buffId) =>
            _buffs.GetValueOrDefault(buffId)?.Grants ?? (IReadOnlyList<NpcBuffGrantData>)[];

        public IReadOnlyList<IModifierInstance> CreateModifiers(string buffId, string source)
        {
            var buff = _buffs.GetValueOrDefault(buffId);
            if (buff == null) return []; // an unknown id: HasBuff is where the caller notices, not here

            return buff.Modifiers
                .Select(entry => ModifiersCreator.CreateModifierInstance(
                    EnumParser.ParseEnum<EntityParameter>(entry.Parameter),
                    EnumParser.ParseEnum<ModifierValueType>(entry.Type),
                    entry.Value,
                    source))
                .ToList();
        }

        private void ApplyData(NpcBuffsData data)
        {
            foreach (var buff in data.Buffs)
                _buffs[buff.Id] = buff;
        }
    }
}
