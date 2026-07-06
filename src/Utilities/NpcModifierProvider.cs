namespace Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Components.NpcModifiers;
    using Core.Data;
    using Core.Data.NpcModifiersData;
    using Core.Interfaces.Entity;

    /// <summary>
    /// Shared NPC modifier provider (used by LootGeneration and Battle): loads
    /// res://Data/NpcModifiers/ and serves copies. Parsing and factories live in Core.
    /// </summary>
    public class NpcModifierProvider : INpcModifierProvider
    {
        private const string NpcModifierDataPath = "res://Data/NpcModifiers/";

        private readonly Dictionary<string, INpcModifier> _npcModifiers = [];
        private readonly INpcModifiersFactory _modifiersFactory = new NpcModifiersFactory();

        public NpcModifierProvider() => LoadDataAsync();

        public INpcModifier GetModifier(string id) => !_npcModifiers.TryGetValue(id, out var modifier)
            ? throw new KeyNotFoundException($"No NPC modifier loaded for '{id}'")
            : modifier.Copy();

        public List<string> GetAllModifierIds() => _npcModifiers.Keys.ToList();

        public IReadOnlyList<INpcModifier> GetAllModifiers() => _npcModifiers.Values.ToList();

        public async void LoadDataAsync()
        {
            try
            {
                await DataLoader.LoadDataFromJson(NpcModifierDataPath, ParseNpcModifiers);
            }
            catch (Exception exception)
            {
                Tracker.TrackException("Failed to load npc modifier data.", exception, this);
            }
        }

        private Task ParseNpcModifiers(string json)
        {
            var data = NpcModifiersParser.Parse(json);
            var modifiers = _modifiersFactory.CreateNpcModifiers(data);
            modifiers.ForEach(mod => _npcModifiers.TryAdd(mod.Id, mod));
            return Task.CompletedTask;
        }
    }
}
