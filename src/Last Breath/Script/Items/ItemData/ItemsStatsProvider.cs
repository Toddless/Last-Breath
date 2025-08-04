namespace LastBreath.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Contracts.Data;
    using Contracts.Enums;
    using Contracts.Interfaces;
    using Godot;
    using Newtonsoft.Json;

    // TODO: this class need more attention
    public class ItemsStatsProvider : IItemDataProvider<ItemStats>
    {
        private readonly Dictionary<WeaponType, WeaponStatsData> _weaponStats = [];
        private readonly Dictionary<JewelleryType, JewelleriesStatsData> _jewelleriesStats = [];
        private readonly Dictionary<ArmorType, ArmorStatsData> _bodyArmorStats = [];

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

        public ItemStats GetItemData(IEquipItem item)
        {
            var itemRarity = item.Rarity;
            var itemAttributeType = item.AttributeType;

            return item.EquipmentPart switch
            {
                EquipmentPart.BodyArmor => GetAttributeArmorStats(ArmorType.BodyArmor, itemRarity, itemAttributeType),
                EquipmentPart.Belt => GetJewelleryStats(JewelleryType.Belt, itemRarity),
                EquipmentPart.Gloves => GetAttributeArmorStats(ArmorType.Gloves, itemRarity, itemAttributeType),
                EquipmentPart.Boots => GetAttributeArmorStats(ArmorType.Boots, itemRarity, itemAttributeType),
                EquipmentPart.Helmet => GetAttributeArmorStats(ArmorType.Helmet, itemRarity, itemAttributeType),
                EquipmentPart.Amulet => GetJewelleryStats(JewelleryType.Amulet, itemRarity),
                EquipmentPart.Cloak => GetArmorStats(ArmorType.Cloak, itemRarity),
                EquipmentPart.Weapon => GetWeaponStats(item),
                EquipmentPart.Ring => GetAttributeJewelleryStats(JewelleryType.Ring, itemRarity, itemAttributeType),
                _ => throw new ArgumentOutOfRangeException(nameof(item))
            };
        }

        private ItemStats GetWeaponStats(IEquipItem item)
        {
            var weaponItem = (IWeaponItem)item;
            if (!_weaponStats.TryGetValue(weaponItem.WeaponType, out var data))
            {
                data = new();
            }

            return data.GetSimpleData(weaponItem.Rarity);
        }

        private ItemStats GetAttributeJewelleryStats(JewelleryType type, Rarity rarity, AttributeType attributeType) => GetJewelleriesData(type).GetAttributeData(attributeType, rarity);

        private ItemStats GetAttributeArmorStats(ArmorType type, Rarity rarity, AttributeType attributeType) => GetArmorData(type).GetAttributeData(attributeType, rarity);

        private ItemStats GetJewelleryStats(JewelleryType type, Rarity rarity) => GetJewelleriesData(type).GetSimpleData(rarity);

        private ItemStats GetArmorStats(ArmorType type, Rarity rarity) => GetArmorData(type).GetSimpleData(rarity);

        private ArmorStatsData GetArmorData(ArmorType type)
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
            foreach (var type in Enum.GetValues<ArmorType>())
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

        private ArmorStatsData CreateArmorData(ArmorType type, string json)
        {
            ArmorStatsData armorData = new();
            switch (type)
            {
                case ArmorType.Cloak:
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

        private static Dictionary<AttributeType, Dictionary<Rarity, ItemStats>> DeserializeAttributeStats(string json)
            => JsonConvert.DeserializeObject<Dictionary<AttributeType, Dictionary<Rarity, ItemStats>>>(json) ?? [];

        private static Dictionary<Rarity, ItemStats> DeserializeItemStats(string json)
            => JsonConvert.DeserializeObject<Dictionary<Rarity, ItemStats>>(json) ?? [];

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
