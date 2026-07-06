namespace Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Data.CraftingData;
    using Core.Data.EquipData;
    using Core.Data.ItemData;
    using Core.Data.LootTable;
    using Core.Data.NpcModifiersData;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Godot;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
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

        public Task ParseLootTables(string json,
            ref Dictionary<Fractions, List<LootTableTierData>> fractionsTables,
            ref Dictionary<EntityType, List<LootTableTierData>> entityTypeTables,
            ref Dictionary<string, List<LootTableTierData>> individualTables,
            ref List<LootTableTierData> basicTable)
        {
            var data = JsonConvert.DeserializeObject<TablesData>(json, s_settings) ?? throw new FileNotFoundException();

            foreach (var lootTable in data.General)
                basicTable.AddRange(lootTable.Tiers);

            foreach (var table in data.Fractions)
            {
                ParseEnum(table.Key, out Fractions result);
                fractionsTables.TryAdd(result, table.Tiers);
            }

            foreach (var lootTable in data.Types)
            {
                ParseEnum(lootTable.Key, out EntityType result);
                entityTypeTables.TryAdd(result, lootTable.Tiers);
            }

            foreach (var lootTable in data.Individual)
                individualTables.TryAdd(lootTable.Key, lootTable.Tiers);

            return Task.CompletedTask;
        }

        public Task<Dictionary<string, Dictionary<string, int>>> ParseEquipItemResources(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipItemResourcesData>(json) ?? throw new InvalidOperationException();
            var resources = data.Resources.ToDictionary(
                e => e.ItemId,
                e => e.Resources.ToDictionary(r => r.ResourceId, r => r.Amount));
            return Task.FromResult(resources);
        }

        public Task<Dictionary<string, List<NpcModifierData>>> ParseNpcModifiers(string json)
        {
            var data = JsonConvert.DeserializeObject<ModifiersData>(json) ?? throw new InvalidOperationException();
            var modifiers = new Dictionary<string, List<NpcModifierData>>();
            foreach (var npcModifiers in data.Mods)
            {
                List<NpcModifierData> mods = npcModifiers.Key switch
                {
                    "scale" => CreateNpcModifier<ScaleModifierData>(npcModifiers.Modifiers),
                    "tierUpgrade" => CreateNpcModifier<TierUpgradeData>(npcModifiers.Modifiers),
                    "guaranteedItems" => CreateNpcModifier<GuaranteedItemsData>(npcModifiers.Modifiers),
                    "tierMultiplier" => CreateNpcModifier<TierMultiplierData>(npcModifiers.Modifiers),
                    "itemEffects" => CreateNpcModifier<ItemEffectData>(npcModifiers.Modifiers),
                    "minRarity" => CreateNpcModifier<MinRarityModifierData>(npcModifiers.Modifiers),
                    "rarityUpgrade" => CreateNpcModifier<RarityUpgradeModifierData>(npcModifiers.Modifiers),
                    _ => []
                };
                modifiers.Add(npcModifiers.Key, mods);
            }

            return Task.FromResult(modifiers);
        }

        public Task ParseEquipItemModifierPools(string json, ref Dictionary<string, List<IModifier>> equipItemModifierPools)
        {
            var data = JsonConvert.DeserializeObject<EquipModifiersPoolRoot>(json, s_settings) ?? throw new InvalidOperationException();
            foreach (var modifierPool in data.Root)
            {
                var itemModifiers = modifierPool.ModifiersPool.Select(modifier =>
                {
                    ParseEnum(modifier.Parameter, out EntityParameter param);
                    ParseEnum(modifier.ModifierType, out ModifierValueType type);
                    var itemModifier = factory.CreateModifier(param, type, modifier.Value, modifier.Weight);
                    itemModifier.Scope = ParseScope(modifier.Scope);
                    return itemModifier;
                }).ToList();

                equipItemModifierPools.TryAdd(modifierPool.Id, itemModifiers);
            }

            return Task.CompletedTask;
        }

        public async Task<List<IItem>> ParseItems(string json)
        {
            var data = JsonConvert.DeserializeObject<ItemDataList>(json, s_settings);
            var items = (data?.Items ?? []).Select(item =>
            {
                ParseEnum<Rarity>(item.Rarity, out var rarity);
                return factory.CreateItem(item.Id, rarity, item.MaxStackSize, item.Tags);
            }).ToList();

            return await Task.FromResult(items);
        }

        public async Task<List<IItem>> ParseEquipItems(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipItemDataList>(json, s_settings);
            var items = new List<IItem>();

            foreach (var item in data?.Items ?? [])
            {
                var baseModifiers = LoadModifiers(item.Implicits);
                var additionalModifiers = LoadModifiers(item.Modifiers);
                ParseEnum<Rarity>(item.Rarity, out var rarity);
                ParseEnum<EquipmentPiece>(item.EquipmentPart, out var equipmentType);

                var newItem = CreateEquipItem(item, equipmentType);
                newItem.Rarity = rarity;
                newItem.UpdateLevel = item.UpdateLevel;
                newItem.MaxUpdateLevel = item.MaxUpdateLevel;
                newItem.SetItemEffect(item.EffectId);
                newItem.SetImplicits(ModifiersCreator.CreateModifierInstances(baseModifiers, newItem.InstanceId));
                newItem.SetModifiers(ModifiersCreator.CreateModifierInstances(additionalModifiers, newItem.InstanceId));
                LoadGrants(item, newItem);
                items.Add(newItem);
            }

            return await Task.FromResult(items);
        }

        public async Task<List<IItem>> ParseRecipes(string json)
        {
            var data = JsonConvert.DeserializeObject<RecipeData>(json, s_settings);
            var recipes = (data?.CraftingRecipes ?? []).Select(recipeData =>
            {
                var requirements = recipeData.Requirements
                    .Select(requirement => factory.CreateRequirement(Enum.Parse<RequirementType>(requirement.Type), requirement.Id, requirement.Amount))
                    .ToList();

                var recipe = factory.CreateRecipe(
                    recipeData.Id,
                    recipeData.ResultItemId,
                    recipeData.Tags,
                    Enum.Parse<Rarity>(recipeData.Rarity),
                    requirements,
                    Enum.Parse<ItemType>(recipeData.ItemType),
                    recipeData.IsOpened,
                    recipeData.OptionalResourceCategories);

                return (IItem)recipe;
            }).ToList();

            return await Task.FromResult(recipes);
        }

        public async Task<List<IItem>> ParseResources(string json)
        {
            var data = JsonConvert.DeserializeObject<ResourcesData>(json, s_settings);
            var categoryDic = new Dictionary<string, IMaterialCategory>();

            foreach (var categoryData in data?.MaterialCategories ?? [])
            {
                var modifiers = LoadMaterialModifiers(categoryData);
                categoryDic[categoryData.Id] = factory.CreateMaterialCategory(modifiers, categoryData.Id);
            }

            var upgrades = LoadUpgradeResources(data?.UpgradeResources ?? []);
            var craftingResources = LoadCraftingResources(categoryDic, data?.CraftingResources ?? []);

            var items = new List<IItem>();
            items.AddRange(upgrades.Cast<IItem>());
            items.AddRange(craftingResources.Cast<IItem>());

            return await Task.FromResult(items);
        }

        private void LoadGrants(EquipItemData itemData, IEquipItem item)
        {
            foreach (var grantData in itemData.Grants)
            {
                ParseEnum<GrantKind>(grantData.Kind, out var kind);
                var grant = factory.CreateGrant(kind, grantData.Id, LoadModifiers(grantData.Modifiers));
                if (grant != null) item.AddGrant(grant);
            }
        }

        private IEquipItem CreateEquipItem(EquipItemData item, EquipmentPiece equipmentType)
        {
            if (equipmentType != EquipmentPiece.Weapon)
                return factory.CreateEquipItem(equipmentType, item.Id, item.Tags);

            ParseEnum<WeaponType>(item.WeaponType, out var weaponType);
            ParseEnum<Handedness>(item.Handedness, out var handedness);
            return factory.CreateWeaponItem(weaponType, handedness, item.Damage, item.CritChance, item.CritDamage, item.Id, item.Tags);
        }

        private List<IModifier> LoadModifiers(List<ItemModifier> modifiers) =>
            modifiers.Select(IModifier (m) =>
            {
                var type = s_typeMap.GetValueOrDefault(m.ModifierType);
                ParseEnum<EntityParameter>(m.Parameter, out var parameter);
                return new Modifier(type, parameter, m.Value) { Scope = ParseScope(m.Scope) };
            }).ToList();

        private List<IUpgradingResource> LoadUpgradeResources(List<UpgradeResourceData> upgradeResourceData) =>
            upgradeResourceData.Select(upgradeResource =>
            {
                ParseEnum<Rarity>(upgradeResource.Rarity, out var rarity);
                ParseEnum<EquipmentCategory>(upgradeResource.Category, out var category);
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
                    Tracker.TrackError("Category not found");
                    GD.Print($"Category not found: {craftingData.Material.CategoryId}");
                    continue;
                }

                var materialModifiers = craftingData.Material.Modifiers.Select(m =>
                {
                    ParseEnum<EntityParameter>(m.Parameter, out var parameter);
                    var materialModifier = factory.CreateMaterialModifier(parameter, s_typeMap.GetValueOrDefault(m.ModifierType), m.BaseValue, m.Weight);
                    materialModifier.Scope = ParseScope(m.Scope);
                    return materialModifier;
                }).ToList();

                ParseEnum<Rarity>(craftingData.Rarity, out var rarity);
                var material = factory.CreateMaterial(materialModifiers, category);
                items.Add(factory.CreateCraftingResource(craftingData.Id, craftingData.MaxStackSize, craftingData.Tags, material, rarity));
            }

            return items;
        }

        public Task<Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<IRequirement>>>> ParseUpgradeCosts(string json)
        {
            var data = JsonConvert.DeserializeObject<UpgradeCostsData>(json, s_settings) ?? throw new InvalidOperationException();
            var costs = new Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<IRequirement>>>
            {
                [CraftingMode.Upgrade] = LoadCategoryRequirements(data.Upgrade),
                [CraftingMode.Recraft] = LoadCategoryRequirements(data.Recraft)
            };
            return Task.FromResult(costs);
        }

        private Dictionary<EquipmentCategory, List<IRequirement>> LoadCategoryRequirements(List<CategoryRequirementsData> categories)
        {
            var result = new Dictionary<EquipmentCategory, List<IRequirement>>();
            foreach (var categoryData in categories)
            {
                ParseEnum<EquipmentCategory>(categoryData.Category, out var category);
                result[category] = categoryData.Requirements
                    .Select(requirement => factory.CreateRequirement(Enum.Parse<RequirementType>(requirement.Type), requirement.Id, requirement.Amount))
                    .ToList();
            }

            return result;
        }

        private List<IModifier> LoadMaterialModifiers(MaterialCategoryData categoryData) =>
            categoryData.Modifiers.Select(m =>
            {
                ParseEnum<EntityParameter>(m.Parameter, out var parameter);
                var materialModifier = factory.CreateMaterialModifier(parameter, s_typeMap.GetValueOrDefault(m.ModifierType), m.BaseValue, m.Weight);
                materialModifier.Scope = ParseScope(m.Scope);
                return materialModifier;
            }).ToList();

        private static List<NpcModifierData> CreateNpcModifier<T>(List<JToken> tokens) where T : NpcModifierData =>
            tokens.Select(t => t.ToObject<T>()).Where(item => item != null).Cast<NpcModifierData>().ToList();

        private static bool ParseEnum<TEnum>(string enumAsString, out TEnum result)
            where TEnum : struct => Enum.TryParse(enumAsString, true, out result);

        private static ModifierScope ParseScope(string? scope)
        {
            ParseEnum(scope ?? string.Empty, out ModifierScope result);
            return result;
        }
    }
}
