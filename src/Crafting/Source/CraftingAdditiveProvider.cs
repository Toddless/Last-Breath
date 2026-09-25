namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Data.CraftingData;
    using Core.Data.GameData;
    using Core.Enums;
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
            {
                // Strict per-entry parse: a typo in minRarity drops the WHOLE entry with a report —
                // a rune silently losing its floor would defraud the player.
                Rarity? minRarity = null;
                if (!string.IsNullOrEmpty(entry.MinRarity))
                {
                    if (!EnumParser.TryParseEnum<Rarity>(entry.MinRarity, out var parsed))
                    {
                        Tracker.TrackError($"Skipping crafting additive '{entry.ResourceId}': '{entry.MinRarity}' is not a valid {nameof(Rarity)}");
                        continue;
                    }

                    minRarity = parsed;
                }

                _effects[entry.ResourceId] = new CraftingAdditiveEffects(entry.UpgradeChanceBonus, entry.ExtraUpgradeLevelChance, entry.RecraftPoolId, minRarity);
            }
        }

        public IReadOnlyCollection<string> KnownAdditiveIds => _effects.Keys;

        public CraftingAdditiveEffects? GetEffects(string resourceId) => _effects.GetValueOrDefault(resourceId);
    }
}
