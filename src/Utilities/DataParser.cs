namespace Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data.CraftingData;
    using Core.Data.EquipData;
    using Core.Data.LootTable;
    using Core.Data.NpcModifiersData;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public abstract class DataParser
    {
        private static readonly Dictionary<string, ModifierType> s_typeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["flat"] = ModifierType.Flat,
            ["add"] = ModifierType.Flat,
            ["additional"] = ModifierType.Flat,
            ["increase"] = ModifierType.Increase,
            ["inc"] = ModifierType.Increase,
            ["mult"] = ModifierType.Multiplicative,
            ["multiplicative"] = ModifierType.Multiplicative,
            ["multi"] = ModifierType.Multiplicative
        };

        private static readonly JsonSerializerSettings s_settings = new() { ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() } };

        public static Task ParseLootTables(string json,
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

        public static Task<Dictionary<string, List<NpcModifierData>>> ParseNpcModifiers(string json)
        {
            var data = JsonConvert.DeserializeObject<ModifiersData>(json) ?? throw new InvalidOperationException();
            var modifiers = new Dictionary<string, List<NpcModifierData>>();
            foreach (NpcModifiersData npcModifiers in data.Mods)
            {
                var allMods = npcModifiers.Modifiers;
                List<NpcModifierData> mods = npcModifiers.Key switch
                {
                    "scale" => CreateModifier<ScaleModifierData>(allMods),
                    "tierUpgrade" => CreateModifier<TierUpgradeData>(allMods),
                    "guaranteedItems" => CreateModifier<GuaranteedItemsData>(allMods),
                    "tierMultiplier" => CreateModifier<TierMultiplierData>(allMods),
                    "itemEffects" => CreateModifier<ItemEffectData>(allMods),
                    "rarityUpgrade" => CreateModifier<RarityUpgradeData>(allMods),
                    _ => []
                };
                modifiers.Add(npcModifiers.Key, mods);
            }

            return Task.FromResult(modifiers);
        }

        public static Task ParseEquipItemModifierPools(
            string json,
            ref Dictionary<string, List<IModifier>> equipItemModifierPools,
            Func<EntityParameter, ModifierType, float, IModifier> modifierFactory)
        {
            var data = JsonConvert.DeserializeObject<EquipModifiersPoolRoot>(json, s_settings) ?? throw new FileNotFoundException();
            foreach (var modifierPool in data.Root)
            {
                List<IModifier> itemModifiers = [];
                foreach (ItemModifier modifier in modifierPool.ModifiersPool)
                {
                    ParseEnum(modifier.Parameter, out EntityParameter param);
                    ParseEnum(modifier.ModifierType, out ModifierType type);
                    itemModifiers.Add(modifierFactory(param, type, modifier.Value));
                }

                equipItemModifierPools.TryAdd(modifierPool.Id, itemModifiers);
            }

            return Task.CompletedTask;
        }

        public static async Task<List<IItem>> ParseResources<TCategory>(
            string json,
            Func<List<IMaterialModifier>, string, TCategory> categoryCreator,
            Func<string, string[], Rarity, EquipmentCategory, int, IUpgradingResource> upgradeResourceFactory,
            Func<EntityParameter, ModifierType, float, float, IMaterialModifier> materialModifierFactory,
            Func<List<IMaterialModifier>, IMaterialCategory, IMaterial> materialFactory,
            Func<string, int, string[], IMaterial, Rarity, ICraftingResource> craftingResourceFactory)
            where TCategory : class, IMaterialCategory
        {
            var data = JsonConvert.DeserializeObject<ResourcesData>(json, s_settings);
            List<IMaterialCategory> materialCategories = [];
            var categoryDic = new Dictionary<string, IMaterialCategory>();

            foreach (var categoryData in data?.MaterialCategories ?? [])
            {
                List<IMaterialModifier> modifiers = await LoadMaterialsModifiers(categoryData, materialModifierFactory);

                var category = categoryCreator.Invoke(modifiers, categoryData.Id);
                materialCategories.Add(category);
                categoryDic[categoryData.Id] = category;
            }

            var items = new List<IItem>();

            var upgrades = await LoadUpgradeResources(data?.UpgradeResources ?? [], upgradeResourceFactory);
            var craftingResources = await LoadCraftingResources(categoryDic, data?.CraftingResources ?? [], materialModifierFactory, materialFactory, craftingResourceFactory);

            items.AddRange(upgrades.Cast<IItem>());
            items.AddRange(craftingResources.Cast<IItem>());

            return await Task.FromResult(items);
        }

        public static async Task<List<TRecipe>> ParseRecipes<TRecipe>(
            string json,
            Func<RequirementType, string, int, IResourceRequirement> resourceRequirementFactory,
            Func<string, string, string[], Rarity, List<IResourceRequirement>, bool, TRecipe> recipeFactory)
            where TRecipe : class, ICraftingRecipe, IItem
        {
            var data = JsonConvert.DeserializeObject<RecipeData>(json, s_settings);

            var recipes = new List<TRecipe>();

            foreach (var recipeData in data?.CraftingRecipes ?? [])
            {
                var requirements = new List<IResourceRequirement>();

                foreach (var reqData in recipeData.MainResources)
                {
                    var type = Enum.Parse<RequirementType>(reqData.Type);
                    var requirement = resourceRequirementFactory.Invoke(type, reqData.ResourceId, reqData.Amount);
                    requirements.Add(requirement);
                }

                var rarity = Enum.Parse<Rarity>(recipeData.Rarity);
                var recipe = recipeFactory.Invoke(
                    recipeData.Id,
                    recipeData.ResultItemId,
                    recipeData.Tags,
                    rarity,
                    requirements,
                    recipeData.IsOpened);

                recipes.Add(recipe);
            }

            return await Task.FromResult(recipes);
        }

        public static async Task<List<IItem>> ParseEquipItems(
            string json,
            Func<EquipmentType, string, Rarity, string[], List<IModifier>, List<IModifier>, string, AttributeType, IEquipItem> itemCreator)
        {
            var data = JsonConvert.DeserializeObject<EquipItemDataList>(json, s_settings);

            var items = new List<IItem>();

            foreach (var item in data?.Items ?? [])
            {
                var baseModifiers = await LoadModifiers(item.BaseModifiers);
                var additionalModifiers = await LoadModifiers(item.AdditionalModifiers);
                ParseEnum<Rarity>(item.Rarity, out var rarity);
                ParseEnum<EquipmentType>(item.EquipmentPart, out var equipmentType);
                ParseEnum<AttributeType>(item.AttributeType, out var attributeType);

                var newItem = itemCreator.Invoke(
                    equipmentType,
                    item.Id,
                    rarity,
                    item.Tags,
                    baseModifiers,
                    additionalModifiers,
                    item.EffectId,
                    attributeType);

                items.Add(newItem);
            }

            return await Task.FromResult(items);
        }

        private static Task<List<IModifier>> LoadModifiers(List<ItemModifier> modifiers)
        {
            var modifiersList = new List<IModifier>();

            foreach (var baseMod in modifiers)
            {
                var modifierType = s_typeMap.GetValueOrDefault(baseMod.ModifierType);

                ParseEnum<EntityParameter>(baseMod.Parameter, out var parameter);
                modifiersList.Add(new Modifier(modifierType, parameter, baseMod.Value));
            }

            return Task.FromResult(modifiersList);
        }

        private static Task<List<IUpgradingResource>> LoadUpgradeResources(
            List<UpgradeResourceData> upgradeResourceData,
            Func<string, string[], Rarity, EquipmentCategory, int, IUpgradingResource> upgradeResourceFactory)
        {
            var items = new List<IUpgradingResource>();
            foreach (var upgradeData in upgradeResourceData)
            {
                ParseEnum<Rarity>(upgradeData.Rarity, out var rarity);
                ParseEnum<EquipmentCategory>(upgradeData.Category, out var category);

                var upgrade = upgradeResourceFactory.Invoke(
                    upgradeData.Id,
                    upgradeData.Tags,
                    rarity,
                    category,
                    upgradeData.MaxStackSize);

                items.Add(upgrade);
            }

            return Task.FromResult(items);
        }

        private static Task<List<ICraftingResource>> LoadCraftingResources(
            Dictionary<string, IMaterialCategory> categories,
            List<CraftingResourceData> resourceData,
            Func<EntityParameter, ModifierType, float, float, IMaterialModifier> materialModifierFactory,
            Func<List<IMaterialModifier>, IMaterialCategory, IMaterial> materialCategoryFactory,
            Func<string, int, string[], IMaterial, Rarity, ICraftingResource> craftingResourceFactory)
        {
            var items = new List<ICraftingResource>();
            foreach (var craftingData in resourceData)
            {
                var materialData = craftingData.Material;
                if (!categories.TryGetValue(materialData.CategoryId, out var category))
                {
                    Tracker.TrackError("Category not found");
                    continue;
                }

                var materialModifiers = new List<IMaterialModifier>();

                foreach (var modifierData in materialData.Modifiers)
                {
                    ParseEnum<EntityParameter>(modifierData.Parameter, out var parameter);
                    var modifierType = s_typeMap.GetValueOrDefault(modifierData.ModifierType);

                    var modifier = materialModifierFactory.Invoke(parameter, modifierType, modifierData.BaseValue,
                        modifierData.Weight);
                    materialModifiers.Add(modifier);
                }

                var materialType = materialCategoryFactory.Invoke(materialModifiers, category);
                ParseEnum<Rarity>(craftingData.Rarity, out var rarity);
                var craftingResource = craftingResourceFactory.Invoke(
                    craftingData.Id,
                    craftingData.MaxStackSize,
                    craftingData.Tags,
                    materialType,
                    rarity);

                items.Add(craftingResource);
            }

            return Task.FromResult(items);
        }

        private static async Task<List<IMaterialModifier>> LoadMaterialsModifiers(
            MaterialCategoryData categoryData,
            Func<EntityParameter, ModifierType, float, float, IMaterialModifier> materialModifierFactory)
        {
            var modifiers = new List<IMaterialModifier>();
            foreach (var modifierData in categoryData.Modifiers)
            {
                ParseEnum<EntityParameter>(modifierData.Parameter, out var parameter);
                var modifierType = s_typeMap.GetValueOrDefault(modifierData.ModifierType);

                var modifier = materialModifierFactory(parameter, modifierType, modifierData.BaseValue, modifierData.Weight);
                modifiers.Add(modifier);
            }

            return await Task.FromResult(modifiers);
        }

        private static List<NpcModifierData> CreateModifier<T>(List<JToken> tokens)
            where T : NpcModifierData
        {
            List<NpcModifierData> result = [];
            foreach (JToken jToken in tokens)
            {
                var item = jToken.ToObject<T>();
                if (item != null) result.Add(item);
            }

            return result;
        }

        private static bool ParseEnum<TEnum>(string enumAsString, out TEnum result)
            where TEnum : struct => Enum.TryParse(enumAsString, true, out result);
    }
}
