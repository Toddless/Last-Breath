namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Crafting;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Newtonsoft.Json;

    public class CraftingAdditiveProvider : ICraftingAdditiveProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, CraftingAdditiveEffects> _effects = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.CraftingAdditives];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<CraftingAdditivesData>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize crafting additives file '{file.FileName}'");

            foreach (var entry in data.Additives)
                _effects[entry.ResourceId] = new CraftingAdditiveEffects(entry.UpgradeChanceBonus, entry.ExtraUpgradeLevelChance, entry.RecraftPoolId);
        }

        public IReadOnlyCollection<string> KnownAdditiveIds => _effects.Keys;

        public CraftingAdditiveEffects? GetEffects(string resourceId) => _effects.GetValueOrDefault(resourceId);
    }
}
