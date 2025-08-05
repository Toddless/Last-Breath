namespace LastBreath.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Godot;

    public class ItemsMediaProvider : IItemDataProvider<ItemMediaData, IEquipItem>
    {
        private readonly Dictionary<ArmorType, ArmorMediaData> _bodyArmorData = [];
        private readonly Dictionary<JewelleryType, JewelleriesMediaData> _jewelleryData = [];
        private readonly Dictionary<WeaponType, ItemResources> _weaponData = [];

        public void LoadData()
        {
            LoadArmorData();
            LoadJewelleriesData();
            LoadWeaponData();
        }

        public ItemMediaData GetItemData(IEquipItem item)
        {
            var itemRarity = item.Rarity;
            var itemAttributeType = item.AttributeType;
            return item.EquipmentPart switch
            {
                EquipmentPart.BodyArmor => GetAttributeArmorMediaData(ArmorType.BodyArmor, itemRarity, itemAttributeType),
                EquipmentPart.Gloves => GetAttributeArmorMediaData(ArmorType.Gloves, itemRarity, itemAttributeType),
                EquipmentPart.Boots => GetAttributeArmorMediaData(ArmorType.Boots, itemRarity, itemAttributeType),
                EquipmentPart.Helmet => GetAttributeArmorMediaData(ArmorType.Helmet, itemRarity, itemAttributeType),
                EquipmentPart.Amulet => GetJewelleryMediaData(JewelleryType.Amulet, itemRarity),
                EquipmentPart.Cloak => GetArmorMediaData(ArmorType.Cloak, itemRarity),
                EquipmentPart.Belt => GetJewelleryMediaData(JewelleryType.Belt, itemRarity),
                EquipmentPart.Weapon => GetWeaponMediaData(item),
                EquipmentPart.Ring => GetAttributeJewelleryMediaData(JewelleryType.Ring, itemRarity, itemAttributeType),
                _ => throw new ArgumentOutOfRangeException(nameof(item))
            };
        }

        private ItemMediaData GetWeaponMediaData(IEquipItem item)
        {
            var weaponItem = (IWeaponItem)item;
            if (!_weaponData.TryGetValue(weaponItem.WeaponType, out var resource))
            {
                resource = new();
            }

            return CreateMediaData(weaponItem.Rarity, resource);
        }

        private ItemMediaData GetAttributeJewelleryMediaData(JewelleryType type, Rarity rarity, AttributeType attributeType) =>
           CreateMediaData(rarity, GetAttributeItemresources(GetJewelleriesMediaData(type).AttributeItemResources, attributeType));

        private ItemMediaData GetAttributeArmorMediaData(ArmorType type, Rarity rarity, AttributeType attributeType) =>
            CreateMediaData(rarity, GetAttributeItemresources(GetBodyArmorMediaData(type).AttributeItemResources, attributeType));

        private ItemMediaData GetJewelleryMediaData(JewelleryType type, Rarity rarity) => CreateMediaData(rarity, GetJewelleriesMediaData(type).SimpeItemResources);

        private ItemMediaData GetArmorMediaData(ArmorType type, Rarity rarity) => CreateMediaData(rarity, GetBodyArmorMediaData(type).SimpeItemResources);

        private ArmorMediaData GetBodyArmorMediaData(ArmorType type)
        {
            if (!_bodyArmorData.TryGetValue(type, out var armorMediaData))
            {
                armorMediaData = new();
            }
            return armorMediaData;
        }

        private JewelleriesMediaData GetJewelleriesMediaData(JewelleryType type)
        {
            if (!_jewelleryData.TryGetValue(type, out var jewelleriesMedia))
            {
                jewelleriesMedia = new();
            }
            return jewelleriesMedia;
        }

        private ItemResources GetAttributeItemresources(Dictionary<AttributeType, ItemResources> dict, AttributeType type)
        {
            if (!dict.TryGetValue(type, out var resource))
            {
                resource = new();
            }
            return resource;
        }

        private void LoadWeaponData()
        {
            foreach (var type in Enum.GetValues<WeaponType>())
            {
                _weaponData.Add(type, LoadResource(ItemsDataPaths.CreateWeaponDataPath($"{type}.tres")));
            }
        }

        private void LoadJewelleriesData()
        {
            foreach (var type in Enum.GetValues<JewelleryType>())
            {
                _jewelleryData.Add(type, CreateJewelleriesMediaData(type));
            }
        }

        private void LoadArmorData()
        {
            foreach (var type in Enum.GetValues<ArmorType>())
            {
                _bodyArmorData.Add(type, CreateArmorMediaData(type));
            }
        }

        private ArmorMediaData CreateArmorMediaData(ArmorType type)
        {
            var armorMediaData = new ArmorMediaData();
            switch (type)
            {
                case ArmorType.Cloak:
                    armorMediaData.SimpeItemResources = LoadResource(ItemsDataPaths.CreateArmorDataPath($"{type}.tres"));
                    return armorMediaData;
                default:
                    armorMediaData.AttributeItemResources = CreateAttributeItemResource(type.ToString(), ItemDataFolder.Armors);
                    return armorMediaData;

            }
        }

        private JewelleriesMediaData CreateJewelleriesMediaData(JewelleryType type)
        {
            var jewelleriesMediaData = new JewelleriesMediaData();

            switch (type)
            {
                case JewelleryType.Ring:
                    jewelleriesMediaData.AttributeItemResources = CreateAttributeItemResource(type.ToString(), ItemDataFolder.Jewelleries);
                    return jewelleriesMediaData;
                default:
                    jewelleriesMediaData.SimpeItemResources = LoadResource(ItemsDataPaths.CreateJewelleryDataPath($"{type}.tres"));
                    return jewelleriesMediaData;
            }
        }

        private Dictionary<AttributeType, ItemResources> CreateAttributeItemResource(string itemType, ItemDataFolder folder)
        {
            Dictionary<AttributeType, ItemResources> res = [];
            foreach (var attributeType in Enum.GetValues<AttributeType>())
            {
                var path = ItemsDataPaths.CreatePathToDataFile(folder, $"{itemType + attributeType}.tres");
                if (string.IsNullOrWhiteSpace(path)) continue;
                res.Add(attributeType, LoadResource(path));
            }

            return res;
        }

        private ItemResources LoadResource(string path) => !string.IsNullOrWhiteSpace(path) ? ResourceLoader.Load<ItemResources>(path) : new();

        private ItemMediaData CreateMediaData(Rarity rarity, ItemResources resources)
            => new()
            {
                Description = resources.Description.GetValueOrDefault(rarity),
                IconTexture = resources.IconTexture.GetValueOrDefault(rarity),
                FullTexture = resources.FullTexture.GetValueOrDefault(rarity),
                Name = resources.Name.GetValueOrDefault(rarity),
                Sound = resources.Sound.GetValueOrDefault(rarity),
            };
    }
}
