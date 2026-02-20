namespace LootGeneration.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Data.NpcModifiersData;
    using Core.Interfaces.Entity;
    using Godot;
    using Utilities;

    public class NpcModifierProvider(INpcModifiersFactory factory) : INpcModifierProvider
    {
        private const string NpcModifierData = "res://Data/NpcModifiers/";
        private readonly Dictionary<string, INpcModifier> _npcModifiers = [];

        public INpcModifier GetModifier(string id) => !_npcModifiers.TryGetValue(id, out var modifier)
            ? throw new KeyNotFoundException()
            : modifier.Copy();


        public List<string> GetAllModifierIds() => _npcModifiers.Keys.ToList();

        public IReadOnlyList<INpcModifier> GetAllModifiers() => _npcModifiers.Values.ToList();

        public async void LoadDataAsync()
        {
            try
            {
                await DataLoader.LoadDataFromJson(NpcModifierData, async s => await ParseNpcModifiers(s));
            }
            catch (Exception exception)
            {
                GD.Print($"Failed to load npc modifier data. {exception.Message},  {exception.StackTrace}");
                Tracker.TrackException($"Failed to load npc modifier data.", exception, this);
            }
        }

        private async Task ParseNpcModifiers(string json)
        {
            var data = await DataParser.ParseNpcModifiers(json);
            var modifiers = factory.CreateNpcModifiers(data);
            modifiers.ForEach(mod => _npcModifiers.TryAdd(mod.Id, mod));
        }
    }
}
