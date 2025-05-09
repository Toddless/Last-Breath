namespace Playground.Script.Items.ItemData
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Enums;

    public partial class ItemsMediaHandler : Node
    {
        private bool _loaded = false;
        private readonly Dictionary<BodyArmorType, ArmorMediaData> _bodyArmorData = [];
        private readonly Dictionary<JewelleryType, JewelleriesMediaData> _jewelleryData = [];
        private readonly Dictionary<WeaponType, ItemResources> _weaponData = [];

        public static ItemsMediaHandler? Inctance { get; private set; } 

        public override void _Ready()
        {
            LoadData();
            Inctance = this;
        }

        public void LoadData()
        {
            if(_loaded) return;
            LoadArmorData();
            LoadJewelleriesData();
            LoadWeaponData();
            _loaded = true;
        }

        public ItemMediaData GetWeaponMediaData(WeaponType type, GlobalRarity rarity)
        {
            if (!_weaponData.TryGetValue(type, out var resource))
            {
                resource = new();
            }

            return CreateMediaData(rarity, resource);
        }

        public ItemMediaData GetAttributeJewelleryMediaData(JewelleryType type, GlobalRarity rarity, AttributeType attributeType) =>
           CreateMediaData(rarity, GetAttributeItemresources(GetJewelleriesMediaData(type).AttributeItemResources, attributeType));

        public ItemMediaData GetAttributeArmorMediaData(BodyArmorType type, GlobalRarity rarity, AttributeType attributeType) =>
            CreateMediaData(rarity, GetAttributeItemresources(GetArmorMediaData(type).AttributeItemResources, attributeType));

        public ItemMediaData GetJewelleryMediaData(JewelleryType type, GlobalRarity rarity) => CreateMediaData(rarity, GetJewelleriesMediaData(type).SimpeItemResources);

        public ItemMediaData GetArmorMediaData(BodyArmorType type, GlobalRarity rarity) => CreateMediaData(rarity, GetArmorMediaData(type).SimpeItemResources);

        private ArmorMediaData GetArmorMediaData(BodyArmorType type)
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
            foreach (var type in Enum.GetValues<BodyArmorType>())
            {
                _bodyArmorData.Add(type, CreateArmorMediaData(type));
            }
        }

        private ArmorMediaData CreateArmorMediaData(BodyArmorType type)
        {
            var armorMediaData = new ArmorMediaData();
            switch (type)
            {
                case BodyArmorType.Cloak:
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
            foreach (var type in Enum.GetValues<AttributeType>())
            {
                var path = ItemsDataPaths.CreatePathToDataFile(folder, $"{itemType + type}.tres");
                if (string.IsNullOrEmpty(path)) continue;
                res.Add(type, LoadResource(path));
            }

            return res;
        }

        private ItemResources LoadResource(string path) => ResourceLoader.Load<ItemResources>(path);

        private ItemMediaData CreateMediaData(GlobalRarity rarity, ItemResources resources)
            => new()
            {
                Description = resources.Description.GetValueOrDefault(rarity),
                Texture = resources.Texture.GetValueOrDefault(rarity),
                Name = resources.Name.GetValueOrDefault(rarity),
                Sound = resources.Sound.GetValueOrDefault(rarity),
            };
    }
}
