namespace LastBreath.DIComponents.Services
{
    using System;
    using Utilities;
    using Core.Enums;
    using Core.Modifiers;
    using Crafting.Source;
    using Core.Interfaces;
    using Newtonsoft.Json;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Crafting;
    using LastBreath.Script.Items;
    using System.Collections.Generic;
    using Newtonsoft.Json.Serialization;
    using Godot;
    using System.IO;

    public class DataParser
    {
        private static readonly string s_assetsPath = "res://Assets/Items/";
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

        private static readonly JsonSerializerSettings s_settings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

        public static async Task<List<IItem>> ParseResources(string json)
        {
            var data = JsonConvert.DeserializeObject<ResourcesData>(json, s_settings);
            var materialCategories = new List<IMaterialCategory>();
            var categoryDic = new Dictionary<string, IMaterialCategory>();

            foreach (var categoryData in data?.MaterialCategories ?? [])
            {
                List<IMaterialModifier> modifiers = await LoadMaterialsModifiers(categoryData);

                var category = new Crafting.Source.MaterialCategory(modifiers, categoryData.Id);
                materialCategories.Add(category);
                categoryDic[categoryData.Id] = category;
            }

            var items = new List<IItem>();

            items.AddRange(LoadUpgradeResources(categoryDic, data?.UpgradeResources ?? []));

            items.AddRange(LoadCraftingResources(categoryDic, data?.CraftingResources ?? []));

            return await Task.FromResult(items);
        }

        public static async Task<List<IItem>> ParseRecipes(string json)
        {
            var data = JsonConvert.DeserializeObject<RecipeData>(json, s_settings);

            var recipes = new List<IItem>();

            foreach (var recipeData in data?.CraftingRecipes ?? [])
            {
                var requirements = new List<IResourceRequirement>();

                foreach (var reqData in recipeData.MainResources)
                {
                    var type = Enum.Parse<RequirementType>(reqData.Type);
                    var requirement = new ResourceRequirement(type, reqData.ResourceId, reqData.Amount);
                    requirements.Add(requirement);
                }

                //  var icon = ResourceLoader.Load<Texture2D>(Path.Combine($"{s_assetsPath}/Recipes", recipeData.Icon));
                var rarity = Enum.Parse<Rarity>(recipeData.Rarity);
                var recipe = new CraftingRecipe(
                    recipeData.Id,
                    recipeData.ResultItemid,
                    recipeData.Tags,
                    null,
                    rarity,
                    requirements,
                    recipeData.IsOpened);

                recipes.Add(recipe);
            }

            return await Task.FromResult(recipes);
        }

        public static async Task<List<IItem>> ParseEquipItems(string json)
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
                var icon = ResourceLoader.Load<Texture2D>(Path.Combine($"{s_assetsPath}/Equip", item.Icon));

                var newItem = new EquipItem(
                    equipmentType,
                    item.Id,
                    icon,
                    rarity,
                    item.Tags,
                    baseModifiers,
                    additionalModifiers,
                    attributeType,
                    item.MaxStackSize,
                    null);

                items.Add(newItem);
            }

            return await Task.FromResult(items);
        }

        private static Task<List<IModifier>> LoadModifiers(List<ItemModifier> modifiers)
        {
            var modifiersList = new List<IModifier>();

            foreach (var baseMod in modifiers)
            {
                if (!s_typeMap.TryGetValue(baseMod.ModifierType, out var modifierType))
                {
                    modifierType = default;
                }
                ParseEnum<Parameter>(baseMod.Parameter, out var parameter);
                modifiersList.Add(new Modifier(modifierType, parameter, baseMod.Value));
            }

            return Task.FromResult(modifiersList);
        }

        private static List<IItem> LoadUpgradeResources(Dictionary<string, IMaterialCategory> categories, List<UpgradeResourceData> upgradeResourceDatas)
        {
            var items = new List<IItem>();
            foreach (var upgradeData in upgradeResourceDatas)
            {
                ParseEnum<Rarity>(upgradeData.Rarity, out var rarity);
                ParseEnum<EquipmentCategory>(upgradeData.Category, out var category);
                //var icon = ResourceLoader.Load<Texture2D>(Path.Combine($"{s_assetsPath}/Resources", upgradeData.Icon));
                var upgrade = new UpgradeResource(
                    upgradeData.Id,
                    upgradeData.Tags,
                    rarity,
                    category,
                    null,
                    upgradeData.MaxStackSize);

                items.Add(upgrade);
            }
            return items;
        }

        private static List<IItem> LoadCraftingResources(Dictionary<string, IMaterialCategory> categories, List<CraftingResourceData> resourceDatas)
        {
            var items = new List<IItem>();
            foreach (var craftingData in resourceDatas)
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
                    ParseEnum<Parameter>(modifierData.Parameter, out var parameter);
                    if (!s_typeMap.TryGetValue(modifierData.ModifierType, out var modifierType))
                    {
                        modifierType = default;
                    }
                    var modifier = new MaterialModifier(parameter, modifierType, modifierData.BaseValue, modifierData.Weight);
                    materialModifiers.Add(modifier);
                }

                var materialType = new MaterialType(materialModifiers, category);
                ParseEnum<Rarity>(craftingData.Rarity, out var rarity);
                //var icon = ResourceLoader.Load<Texture2D>(Path.Combine(s_assetsPath, craftingData.Icon));
                var craftingResource = new CraftingResource(
                    craftingData.Id,
                    craftingData.MaxStackSize,
                    craftingData.Tags,
                    null,
                    materialType,
                    rarity);

                items.Add(craftingResource);
            }

            return items;
        }

        private static async Task<List<IMaterialModifier>> LoadMaterialsModifiers(MaterialCategoryData categoryData)
        {
            var modifiers = new List<IMaterialModifier>();
            foreach (var modifierData in categoryData.Modifiers)
            {
                ParseEnum<Parameter>(modifierData.Parameter, out var parameter);
                if (!s_typeMap.TryGetValue(modifierData.ModifierType, out var modifierType))
                {
                    modifierType = default;
                }
                var modifier = new MaterialModifier(parameter, modifierType, modifierData.BaseValue, modifierData.Weight);
                modifiers.Add(modifier);
            }

            return await Task.FromResult(modifiers);
        }

        private static bool ParseEnum<TEnum>(string enumAsString, out TEnum result)
         where TEnum : struct => Enum.TryParse(enumAsString, true, out result);

        private record ResourcesData(List<MaterialCategoryData> MaterialCategories, List<UpgradeResourceData> UpgradeResources, List<CraftingResourceData> CraftingResources);
        private record CraftingResourceData(string Id, MaterialData Material, int MaxStackSize, string[] Tags, string Icon, string Rarity);
        private record UpgradeResourceData(string Id, string[] Tags, string Rarity, string Category, string Icon, int MaxStackSize);
        private record MaterialModifierData(string Parameter, string ModifierType, float BaseValue, int Weight);
        private record MaterialData(string Id, string CategoryId, List<MaterialModifierData> Modifiers);
        private record MaterialCategoryData(string Id, List<MaterialModifierData> Modifiers);
        private record ResourceRequirementData(string Type, string ResourceId, int Amount);
        private record CraftingRecipeData(string Id, string ResultItemid, string[] Tags, string Icon, string Rarity, bool IsOpened, List<ResourceRequirementData> MainResources);
        private record RecipeData(List<CraftingRecipeData> CraftingRecipes);

        private record EquipItemDataList(List<EquipItemData> Items);
        private record EquipItemData(string Id, string EquipmentPart, int MaxStackSize, string Icon, string Rarity, string[] Tags, string AttributeType, string Skill, int UpdateLevel, int MaxUpdateLevel, List<ItemModifier> BaseModifiers, List<ItemModifier> AdditionalModifiers);
        private record ItemModifier(string Parameter, string ModifierType, float Value);

    }
}
