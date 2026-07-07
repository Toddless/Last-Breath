namespace Core.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using CraftingData;
    using EquipData;
    using ItemData;
    using LootTable;
    using Enums;
    using Interfaces;
    using Modifiers;
    using Crafting;
    using Items;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class DataParser(IItemGameDataFactory factory) : IDataParser
    {
        private static readonly JsonSerializerSettings s_settings = new() { ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() } };

        private static readonly Dictionary<string, ModifierValueType> s_typeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["flat"] = ModifierValueType.Flat,
            ["add"] = ModifierValueType.Flat,
            ["additional"] = ModifierValueType.Flat,
            ["increase"] = ModifierValueType.Increase,
            ["inc"] = ModifierValueType.Increase,
            ["mult"] = ModifierValueType.Multiplicative,
            ["multiplicative"] = ModifierValueType.Multiplicative,
            ["multi"] = ModifierValueType.Multiplicative
        };

        public LootTablesParseResult ParseLootTables(string json)
        {
            var data = JsonConvert.DeserializeObject<TablesData>(json, s_settings)
                       ?? throw new InvalidOperationException("Failed to deserialize loot tables");
            var result = new LootTablesParseResult();

            foreach (var lootTable in data.General)
                result.BasicTable.AddRange(lootTable.Tiers);

            foreach (var table in data.Fractions)
                result.FractionTables.TryAdd(DataParse.ParseEnum<Fractions>(table.Key), table.Tiers);

            foreach (var lootTable in data.Types)
                result.EntityTypeTables.TryAdd(DataParse.ParseEnum<EntityType>(lootTable.Key), lootTable.Tiers);

            foreach (var lootTable in data.Individual)
                result.IndividualTables.TryAdd(lootTable.Key, lootTable.Tiers);

            return result;
        }

        public Dictionary<string, Dictionary<string, int>> ParseEquipItemResources(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipItemResourcesData>(json) ?? throw new InvalidOperationException();
            return data.Resources.ToDictionary(
                e => e.ItemId,
                e => e.Resources.ToDictionary(r => r.ResourceId, r => r.Amount));
        }

        public Dictionary<string, List<IModifier>> ParseEquipItemModifierPools(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipModifiersPoolRoot>(json, s_settings) ?? throw new InvalidOperationException();
            var pools = new Dictionary<string, List<IModifier>>();
            foreach (var modifierPool in data.Root)
            {
                var itemModifiers = new List<IModifier>();
                foreach (var modifier in modifierPool.ModifiersPool)
                {
                    if (!TryParseModifier(modifierPool.Id, modifier.Parameter, modifier.ModifierType, out var parameter, out var type)) continue;

                    var itemModifier = factory.CreateModifier(parameter, type, modifier.Value, modifier.Weight);
                    itemModifier.Scope = DataParse.ParseEnumOrDefault<ModifierScope>(modifier.Scope);
                    itemModifiers.Add(itemModifier);
                }

                pools.TryAdd(modifierPool.Id, itemModifiers);
            }

            return pools;
        }

        public List<IItem> ParseItems(string json)
        {
            var data = JsonConvert.DeserializeObject<ItemDataList>(json, s_settings);
            return (data?.Items ?? [])
                .Select(item => factory.CreateItem(item.Id, DataParse.ParseEnum<Rarity>(item.Rarity), item.MaxStackSize, item.Tags))
                .ToList();
        }

        public List<IItem> ParseEquipItems(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipItemDataList>(json, s_settings);
            var items = new List<IItem>();

            foreach (var item in data?.Items ?? [])
            {
                var baseModifiers = LoadModifiers(item.Implicits);
                var additionalModifiers = LoadModifiers(item.Modifiers);
                var equipmentType = DataParse.ParseEnum<EquipmentPiece>(item.EquipmentPart);

                var newItem = CreateEquipItem(item, equipmentType);
                newItem.Rarity = DataParse.ParseEnum<Rarity>(item.Rarity);
                newItem.UpdateLevel = item.UpdateLevel;
                newItem.MaxUpdateLevel = item.MaxUpdateLevel;
                newItem.SetItemEffect(item.EffectId);
                newItem.SetImplicits(ModifiersCreator.CreateModifierInstances(baseModifiers, newItem.InstanceId));
                newItem.SetModifiers(ModifiersCreator.CreateModifierInstances(additionalModifiers, newItem.InstanceId));
                LoadGrants(item, newItem);
                items.Add(newItem);
            }

            return items;
        }

        public List<IItem> ParseRecipes(string json)
        {
            var data = JsonConvert.DeserializeObject<RecipeData>(json, s_settings);
            return (data?.CraftingRecipes ?? []).Select(recipeData =>
            {
                var requirements = recipeData.Requirements
                    .Select(requirement => factory.CreateRequirement(DataParse.ParseEnum<RequirementType>(requirement.Type), requirement.Id, requirement.Amount))
                    .ToList();

                var recipe = factory.CreateRecipe(
                    recipeData.Id,
                    recipeData.ResultItemId,
                    recipeData.Tags,
                    DataParse.ParseEnum<Rarity>(recipeData.Rarity),
                    requirements,
                    DataParse.ParseEnum<ItemType>(recipeData.ItemType),
                    recipeData.IsOpened,
                    recipeData.OptionalResourceCategories);

                return (IItem)recipe;
            }).ToList();
        }

        public List<IItem> ParseResources(string json)
        {
            var data = JsonConvert.DeserializeObject<ResourcesData>(json, s_settings);
            var categoryDic = new Dictionary<string, IMaterialCategory>();

            foreach (var categoryData in data?.MaterialCategories ?? [])
            {
                var modifiers = LoadMaterialModifiers(categoryData.Id, categoryData.Modifiers);
                categoryDic[categoryData.Id] = factory.CreateMaterialCategory(modifiers, categoryData.Id);
            }

            var upgrades = LoadUpgradeResources(data?.UpgradeResources ?? []);
            var craftingResources = LoadCraftingResources(categoryDic, data?.CraftingResources ?? []);

            var items = new List<IItem>();
            items.AddRange(upgrades.Cast<IItem>());
            items.AddRange(craftingResources.Cast<IItem>());

            return items;
        }

        public Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<IRequirement>>> ParseUpgradeCosts(string json)
        {
            var data = JsonConvert.DeserializeObject<UpgradeCostsData>(json, s_settings) ?? throw new InvalidOperationException();
            return new Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<IRequirement>>>
            {
                [CraftingMode.Upgrade] = LoadCategoryRequirements(data.Upgrade),
                [CraftingMode.Recraft] = LoadCategoryRequirements(data.Recraft)
            };
        }

        private void LoadGrants(EquipItemData itemData, IEquipItem item)
        {
            foreach (var grantData in itemData.Grants)
            {
                var kind = DataParse.ParseEnum<GrantKind>(grantData.Kind);
                var grant = factory.CreateGrant(kind, grantData.Id, LoadModifiers(grantData.Modifiers));
                if (grant != null) item.AddGrant(grant);
            }
        }

        private IEquipItem CreateEquipItem(EquipItemData item, EquipmentPiece equipmentType)
        {
            if (equipmentType != EquipmentPiece.Weapon)
                return factory.CreateEquipItem(equipmentType, item.Id, item.Tags);

            var weaponType = DataParse.ParseEnum<WeaponType>(item.WeaponType);
            var handedness = DataParse.ParseEnum<Handedness>(item.Handedness);
            return factory.CreateWeaponItem(weaponType, handedness, item.Damage, item.CritChance, item.CritDamage, item.Id, item.Tags);
        }

        private List<IModifier> LoadModifiers(List<ItemModifier> modifiers)
        {
            var result = new List<IModifier>();
            foreach (var m in modifiers)
            {
                if (!TryParseModifier("item", m.Parameter, m.ModifierType, out var parameter, out var type)) continue;

                result.Add(new Modifier(type, parameter, m.Value)
                {
                    Scope = DataParse.ParseEnumOrDefault<ModifierScope>(m.Scope)
                });
            }

            return result;
        }

        private List<IUpgradingResource> LoadUpgradeResources(List<UpgradeResourceData> upgradeResourceData) =>
            upgradeResourceData.Select(upgradeResource =>
            {
                var rarity = DataParse.ParseEnum<Rarity>(upgradeResource.Rarity);
                var category = DataParse.ParseEnum<EquipmentCategory>(upgradeResource.Category);
                return factory.CreateUpgradeResource(upgradeResource.Id, upgradeResource.Tags, rarity, category, upgradeResource.MaxStackSize);
            }).ToList();

        private List<ICraftingResource> LoadCraftingResources(
            Dictionary<string, IMaterialCategory> categories,
            List<CraftingResourceData> resourceData)
        {
            var items = new List<ICraftingResource>();
            foreach (var craftingData in resourceData)
            {
                if (!categories.TryGetValue(craftingData.Material.CategoryId, out var category))
                {
                    Tracker.TrackError($"Category not found: {craftingData.Material.CategoryId}");
                    continue;
                }

                var materialModifiers = LoadMaterialModifiers(craftingData.Id, craftingData.Material.Modifiers);
                var rarity = DataParse.ParseEnum<Rarity>(craftingData.Rarity);
                var material = factory.CreateMaterial(materialModifiers, category);
                items.Add(factory.CreateCraftingResource(craftingData.Id, craftingData.MaxStackSize, craftingData.Tags, material, rarity));
            }

            return items;
        }

        private Dictionary<EquipmentCategory, List<IRequirement>> LoadCategoryRequirements(List<CategoryRequirementsData> categories)
        {
            var result = new Dictionary<EquipmentCategory, List<IRequirement>>();
            foreach (var categoryData in categories)
            {
                var category = DataParse.ParseEnum<EquipmentCategory>(categoryData.Category);
                result[category] = categoryData.Requirements
                    .Select(requirement => factory.CreateRequirement(DataParse.ParseEnum<RequirementType>(requirement.Type), requirement.Id, requirement.Amount))
                    .ToList();
            }

            return result;
        }

        private List<IModifier> LoadMaterialModifiers(string context, List<MaterialModifierData> modifiers)
        {
            var result = new List<IModifier>();
            foreach (var m in modifiers)
            {
                if (!TryParseModifier(context, m.Parameter, m.ModifierType, out var parameter, out var type)) continue;

                var materialModifier = factory.CreateMaterialModifier(parameter, type, m.BaseValue, m.Weight);
                materialModifier.Scope = DataParse.ParseEnumOrDefault<ModifierScope>(m.Scope);
                result.Add(materialModifier);
            }

            return result;
        }

        /// <summary>
        /// Per-entry tolerance: a bad modifier is reported and dropped so one typo doesn't
        /// discard the whole file. Structural errors still fail the file upstream.
        /// </summary>
        private static bool TryParseModifier(string context, string parameterValue, string modifierTypeValue,
            out EntityParameter parameter, out ModifierValueType type)
        {
            parameter = default;
            type = default;
            try
            {
                parameter = DataParse.ParseEnum<EntityParameter>(parameterValue);
                type = ParseModifierType(modifierTypeValue);
                return true;
            }
            catch (FormatException e)
            {
                Tracker.TrackError($"Skipping modifier of '{context}': {e.Message}");
                return false;
            }
        }

        private static ModifierValueType ParseModifierType(string value) =>
            s_typeMap.TryGetValue(value, out var type)
                ? type
                : throw new FormatException($"'{value}' is not a valid modifier value type");
    }
}
