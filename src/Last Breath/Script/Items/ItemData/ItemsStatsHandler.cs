namespace Playground.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Playground.Script.Enums;

    public class ItemsStatsHandler : BaseItemHandler, IItemStatsHandler
    {
        private readonly Dictionary<WeaponType, WeaponStatsData> _weaponStats = [];
        private readonly Dictionary<JewelleryType, JewelleriesStatsData> _jewelleriesStats = [];
        private readonly Dictionary<BodyArmorType, ArmorStatsData> _bodyArmorStats = [];

        public void LoadData()
        {
            LoadJewelleryData();
            LoadWeaponData();
            LoadArmorData();
        }

        public ItemStats GetWeaponStats(WeaponType type, GlobalRarity rarity)
        {
            if (!_weaponStats.TryGetValue(type, out var data))
            {
                data = new();
            }

            return data.GetSimpleStats(rarity);
        }

        public ItemStats GetAttributeJewelleryStats(JewelleryType type, GlobalRarity rarity, AttributeType attributeType) => GetJewelleriesData(type).GetAttributeStats(attributeType, rarity);

        public ItemStats GetAttributeBodyArmorStats(BodyArmorType type, GlobalRarity rarity, AttributeType attributeType) => GetArmorData(type).GetAttributeStats(attributeType, rarity);

        public ItemStats GetJewelleryStats(JewelleryType type, GlobalRarity rarity) => GetJewelleriesData(type).GetSimpleStats(rarity);

        public ItemStats GetBodyArmorStats(BodyArmorType type, GlobalRarity rarity) => GetArmorData(type).GetSimpleStats(rarity);

        private ArmorStatsData GetArmorData(BodyArmorType type)
        {
            if (!_bodyArmorStats.TryGetValue(type, out var data))
            {
                data = new();
            }
            return data;
        }

        private JewelleriesStatsData GetJewelleriesData(JewelleryType type)
        {
            if (!_jewelleriesStats.TryGetValue(type, out var data))
            {
                data = new();
            }
            return data;
        }

        private void LoadWeaponData()
        {
            foreach (var type in Enum.GetValues<WeaponType>())
            {
                var json = GetJson(Weapons, $"{type}.json");
                if (string.IsNullOrWhiteSpace(json))
                {
                    // TODO: Log
                    continue;
                }
                WeaponStatsData weaponData = new() { SimpleStats = DeserializeItemStats(json) };
                _weaponStats.Add(type, weaponData);
            }
        }

        private void LoadArmorData()
        {
            foreach (var type in Enum.GetValues<BodyArmorType>())
            {
                var json = GetJson(Armors, $"{type}.json");
                if (string.IsNullOrWhiteSpace(json))
                {
                    // TODO: Log
                    continue;
                }
                _bodyArmorStats.Add(type, CreateArmorData(type, json));
            }
        }

        private void LoadJewelleryData()
        {
            foreach (var type in Enum.GetValues<JewelleryType>())
            {
                var json = GetJson(Jewelleries, $"{type}.json");
                if (string.IsNullOrWhiteSpace(json))
                {
                    // TODO: Log
                    continue;
                }
                _jewelleriesStats.Add(type, CreateJewelleriesData(type, json));
            }
        }

        private ArmorStatsData CreateArmorData(BodyArmorType type, string json)
        {
            ArmorStatsData armorData = new();
            switch (type)
            {
                case BodyArmorType.Cloak:
                    armorData.SimpleStats = DeserializeItemStats(json);
                    return armorData;
                default:
                    armorData.AttributeStats = DeserializeAttributeStats(json);
                    return armorData;

            }
        }

        private JewelleriesStatsData CreateJewelleriesData(JewelleryType type, string json)
        {
            JewelleriesStatsData jewelleriesData = new();
            switch (type)
            {
                case JewelleryType.Ring:
                    jewelleriesData.AttributeStats = DeserializeAttributeStats(json);
                    return jewelleriesData;
                default:
                    jewelleriesData.SimpleStats = DeserializeItemStats(json);
                    return jewelleriesData;
            }
        }

        private static Dictionary<AttributeType, Dictionary<GlobalRarity, ItemStats>> DeserializeAttributeStats(string json)
            => JsonConvert.DeserializeObject<Dictionary<AttributeType, Dictionary<GlobalRarity, ItemStats>>>(json) ?? [];

        private static Dictionary<GlobalRarity, ItemStats> DeserializeItemStats(string json)
            => JsonConvert.DeserializeObject<Dictionary<GlobalRarity, ItemStats>>(json) ?? [];
    }
}
