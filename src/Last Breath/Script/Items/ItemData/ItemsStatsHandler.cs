namespace Playground.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Godot;
    using Newtonsoft.Json;
    using Playground.Script.Enums;

    public class ItemsStatsHandler : IItemStatsHandler
    {
        private readonly Dictionary<WeaponType, WeaponStatsData> _weaponStats = [];
        private readonly Dictionary<JewelleryType, JewelleriesStatsData> _jewelleriesStats = [];
        private readonly Dictionary<BodyArmorType, ArmorStatsData> _bodyArmorStats = [];

        public void LoadData()
        {
            // For it to work properly, I need to put the JSONs with the data outside the res:// folder.(e.ge %APPDATA%/Godot/app_userdata/Game/Data)
            var userDir = ProjectSettings.GlobalizePath("user://");
            var userDataPath = Path.Combine(userDir, "Data");

            if (!Directory.Exists(userDataPath))
            {
                Directory.CreateDirectory(userDataPath);
                CopyDirectory(ProjectSettings.GlobalizePath("res://Data"), userDataPath);
            }

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

            return data.GetSimpleData(rarity);
        }

        public ItemStats GetAttributeJewelleryStats(JewelleryType type, GlobalRarity rarity, AttributeType attributeType) => GetJewelleriesData(type).GetAttributeData(attributeType, rarity);

        public ItemStats GetAttributeBodyArmorStats(BodyArmorType type, GlobalRarity rarity, AttributeType attributeType) => GetArmorData(type).GetAttributeData(attributeType, rarity);

        public ItemStats GetJewelleryStats(JewelleryType type, GlobalRarity rarity) => GetJewelleriesData(type).GetSimpleData(rarity);

        public ItemStats GetBodyArmorStats(BodyArmorType type, GlobalRarity rarity) => GetArmorData(type).GetSimpleData(rarity);

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
                var json = ItemsDataPaths.GetWeaponDataFromJsonFile($"{type}.json");
                if (string.IsNullOrWhiteSpace(json))
                {
                    // TODO: Log
                    continue;
                }
                WeaponStatsData weaponData = new() { SimpleData = DeserializeItemStats(json) };
                _weaponStats.Add(type, weaponData);
            }
        }

        private void LoadArmorData()
        {
            foreach (var type in Enum.GetValues<BodyArmorType>())
            {
                var json = ItemsDataPaths.GetArmorDataFromJsonFile($"{type}.json");
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
                var json = ItemsDataPaths.GetJewelleriesDataFromJsonFile($"{type}.json");
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
                    armorData.SimpleData = DeserializeItemStats(json);
                    return armorData;
                default:
                    armorData.AttributeData = DeserializeAttributeStats(json);
                    return armorData;

            }
        }

        private JewelleriesStatsData CreateJewelleriesData(JewelleryType type, string json)
        {
            JewelleriesStatsData jewelleriesData = new();
            switch (type)
            {
                case JewelleryType.Ring:
                    jewelleriesData.AttributeData = DeserializeAttributeStats(json);
                    return jewelleriesData;
                default:
                    jewelleriesData.SimpleData = DeserializeItemStats(json);
                    return jewelleriesData;
            }
        }

        private static Dictionary<AttributeType, Dictionary<GlobalRarity, ItemStats>> DeserializeAttributeStats(string json)
            => JsonConvert.DeserializeObject<Dictionary<AttributeType, Dictionary<GlobalRarity, ItemStats>>>(json) ?? [];

        private static Dictionary<GlobalRarity, ItemStats> DeserializeItemStats(string json)
            => JsonConvert.DeserializeObject<Dictionary<GlobalRarity, ItemStats>>(json) ?? [];

        private void CopyDirectory(string srcDir, string userDataPath)
        {
            foreach (var dir in Directory.GetDirectories(srcDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(srcDir, userDataPath));

            foreach (var file in Directory.GetFiles(srcDir, "*.json", SearchOption.AllDirectories))
            {
                var dest = file.Replace(srcDir, userDataPath);
                File.Copy(file, dest, overwrite: true);
            }
        }
    }
}
