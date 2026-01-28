namespace LootGeneration.Services
{
    using Test;
    using Utilities;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;
    using Core.Data.NpcModifiersData;

    public class NpcModifierProvider(INpcModifiersFactory factory)
    {
        private const string NpcModifierData = "res://Data/NpcModifiers/";
        private readonly Dictionary<string, INpcModifier> _npcModifiers = [];

        public INpcModifier GetModifier(string id) => !_npcModifiers.TryGetValue(id, out var modifier)
            ? throw new KeyNotFoundException()
            : modifier.Copy();


        public async Task LoadDataAsync()
        {
            await DataLoader.LoadDataFromJson(NpcModifierData, async s => await ParseNpcModifiers(s));
        }

        private async Task ParseNpcModifiers(string json)
        {
            var data = await DataParser.ParseNpcModifiers(json);
            var modifiers = factory.CreateNpcModifiers(data);
            modifiers.ForEach(mod => _npcModifiers.TryAdd(mod.Id, mod));
        }
    }
}
