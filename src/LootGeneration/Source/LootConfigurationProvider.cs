namespace LootGeneration.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Enums;

    /// <summary>Data-backed <see cref="ILootConfiguration"/>: consumes the LootConfiguration catalog.
    /// A project can override the shared config by shipping its own file in the project data root.</summary>
    public class LootConfigurationProvider(IDataParser dataParser) : ILootConfiguration, IGameDataParticipant
    {
        private ILootConfiguration? _configuration;

        public IReadOnlyList<string> Catalogs => [DataCatalog.LootConfiguration];

        public int[] TierPrices => Configuration.TierPrices;
        public float[] BaseTierChances => Configuration.BaseTierChances;
        public float[] BaseRarityChances => Configuration.BaseRarityChances;
        public float LvlCoefficient => Configuration.LvlCoefficient;
        public float EquipItemEffectChance => Configuration.EquipItemEffectChance;
        public float ItemModifierMultiplier => Configuration.ItemModifierMultiplier;
        public int MaxItemsPerKill => Configuration.MaxItemsPerKill;
        public Dictionary<EntityType, float> BaseBudget => Configuration.BaseBudget;
        public Dictionary<Rarity, float> RarityMultipliers => Configuration.RarityMultipliers;

        private ILootConfiguration Configuration => _configuration ?? throw new InvalidOperationException(
            $"Loot configuration is not loaded — no file found in the '{DataCatalog.LootConfiguration}' catalog.");

        public void Apply(string catalog, GameDataFile file)
        {
            var parsed = dataParser.ParseLootConfiguration(file.Json);
            _configuration = new LootConfiguration(
                parsed.TierPrices,
                parsed.BaseTierChances,
                parsed.BaseRarityChances,
                parsed.LevelCoefficient,
                parsed.EquipItemEffectChance,
                parsed.ItemModifierMultiplier,
                parsed.MaxItemsPerKill,
                parsed.BaseBudget,
                parsed.RarityMultipliers);
        }
    }
}
