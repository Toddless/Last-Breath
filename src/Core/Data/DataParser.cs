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

    /// <summary>Turns raw catalog JSON into domain objects — items, recipes, resources, modifier pools,
    /// upgrade costs. Owns the shared "parameter" namespace resolution (<see cref="Enums.EntityParameter"/>
    /// vs <see cref="Enums.ContextParameter"/>) and per-entry tolerance (a bad line is reported and dropped,
    /// not fatal). The concrete objects themselves are built through the injected <see cref="IItemGameDataFactory"/>,
    /// so each project can supply its own item flavours while sharing this parsing logic.</summary>
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

        // Turns parsed descriptors into fresh item lines (authored implicits/modifiers on parse).
        private readonly IModifierMaterializer _materializer = new ModifierMaterializer();

        public LootTablesParseResult ParseLootTables(string json)
        {
            var data = JsonConvert.DeserializeObject<TablesData>(json, s_settings)
                       ?? throw new InvalidOperationException("Failed to deserialize loot tables");
            var result = new LootTablesParseResult();

            foreach (var lootTable in data.General)
                result.BasicTable.AddRange(lootTable.Tiers);

            foreach (var table in data.Fractions)
                result.FractionTables.TryAdd(EnumParser.ParseEnum<Fractions>(table.Key), table.Tiers);

            foreach (var lootTable in data.Types)
                result.EntityTypeTables.TryAdd(EnumParser.ParseEnum<EntityType>(lootTable.Key), lootTable.Tiers);

            foreach (var lootTable in data.Individual)
                result.IndividualTables.TryAdd(lootTable.Key, lootTable.Tiers);

            return result;
        }

        public LootConfigurationParseResult ParseLootConfiguration(string json)
        {
            var data = JsonConvert.DeserializeObject<LootConfigurationData>(json, s_settings)
                       ?? throw new InvalidOperationException("Failed to deserialize loot configuration");

            if (data.TierPrices.Length == 0)
                throw new InvalidOperationException("Loot configuration must define tierPrices.");
            if (data.BaseTierChances.Length != data.TierPrices.Length)
                throw new InvalidOperationException(
                    $"Loot configuration mismatch: {data.TierPrices.Length} tierPrices but {data.BaseTierChances.Length} baseTierChances.");

            return new LootConfigurationParseResult(
                data.TierPrices,
                data.BaseTierChances,
                data.BaseRarityChances,
                data.LevelCoefficient,
                data.EquipItemEffectChance,
                data.ItemModifierMultiplier,
                data.MaxItemsPerKill,
                data.BaseBudget.ToDictionary(kvp => EnumParser.ParseEnum<EntityType>(kvp.Key), kvp => kvp.Value),
                data.RarityMultipliers.ToDictionary(kvp => EnumParser.ParseEnum<Rarity>(kvp.Key), kvp => kvp.Value));
        }

        public Dictionary<string, Dictionary<string, int>> ParseEquipItemResources(string json)
        {
            // The shipped file is a bare array — deserializing the object wrapper here made the
            // whole catalog silently fail to load (only visible as a Tracker line).
            var data = JsonConvert.DeserializeObject<List<EquipItemResources>>(json) ?? throw new InvalidOperationException();
            return data.ToDictionary(
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
                    if (modifier.Parts.Count > 0)
                    {
                        var composite = LoadCompositeModifier(modifierPool.Id, modifier);
                        if (composite != null) itemModifiers.Add(composite);
                        continue;
                    }

                    if (!TryParseModifier(modifierPool.Id, modifier.Parameter, modifier.ModifierType, out var parameter, out var type)) continue;

                    var itemModifier = factory.CreateModifier(parameter, type, modifier.Value, modifier.Weight);
                    itemModifier.Scope = EnumParser.ParseEnumOrDefault<ModifierScope>(modifier.Scope);
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
                .Select(item => factory.CreateItem(item.Id, EnumParser.ParseEnum<Rarity>(item.Rarity), item.MaxStackSize, item.Tags))
                .ToList();
        }

        public List<IItem> ParseEquipItems(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipItemDataList>(json, s_settings);
            var items = new List<IItem>();

            foreach (var item in data?.Items ?? [])
            {
                var equipmentType = EnumParser.ParseEnum<EquipmentPiece>(item.EquipmentPart);
                var newItem = CreateEquipItem(item, equipmentType);
                newItem.Rarity = EnumParser.ParseEnum<Rarity>(item.Rarity);
                newItem.UpdateLevel = item.UpdateLevel;
                newItem.MaxUpdateLevel = item.MaxUpdateLevel;

                var implicits = MaterializeLines(item.Implicits, newItem.InstanceId);
                newItem.SetImplicits(implicits.Entities);
                newItem.SetContextImplicits(implicits.Contexts);

                var modifiers = MaterializeLines(item.Modifiers, newItem.InstanceId);
                newItem.SetModifiers(modifiers.Entities);
                newItem.SetContextModifiers(modifiers.Contexts);

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
                    .Select(requirement => factory.CreateRequirement(EnumParser.ParseEnum<RequirementType>(requirement.Type), requirement.Id, requirement.Amount))
                    .ToList();

                var recipe = factory.CreateRecipe(
                    recipeData.Id,
                    recipeData.ResultItemId,
                    recipeData.Tags,
                    EnumParser.ParseEnum<Rarity>(recipeData.Rarity),
                    requirements,
                    EnumParser.ParseEnum<ItemType>(recipeData.ItemType),
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
                var descriptors = LoadDescriptors(categoryData.Modifiers, categoryData.Id);
                categoryDic[categoryData.Id] = factory.CreateMaterialCategory(descriptors, categoryData.Id);
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
                [CraftingMode.Recraft] = LoadCategoryRequirements(data.Recraft),
                [CraftingMode.Ascend] = LoadCategoryRequirements(data.Ascend)
            };
        }

        private void LoadGrants(EquipItemData itemData, IEquipItem item)
        {
            foreach (var grantData in itemData.Grants)
            {
                var kind = EnumParser.ParseEnum<GrantKind>(grantData.Kind);
                var grant = factory.CreateGrant(kind, grantData.Id, LoadModifiers(grantData.Modifiers), grantData.Properties);
                if (grant != null) item.AddGrant(grant);
            }
        }

        private IEquipItem CreateEquipItem(EquipItemData item, EquipmentPiece equipmentType)
        {
            if (equipmentType != EquipmentPiece.Weapon)
                return factory.CreateEquipItem(equipmentType, item.Id, item.Tags);

            var weaponType = EnumParser.ParseEnum<WeaponType>(item.WeaponType);
            var handedness = EnumParser.ParseEnum<Handedness>(item.Handedness);
            return factory.CreateWeaponItem(weaponType, handedness, item.Damage, item.CritChance, item.CritDamage, item.Id, item.Tags);
        }

        /// <summary>Item and material lines share one "parameter" namespace: a name resolves as
        /// <see cref="EntityParameter"/> first, then as <see cref="ContextParameter"/> (pipeline knobs).
        /// Unknown in both → the entry is reported and dropped, per the usual tolerance rules. One parser feeds
        /// both authored item lines and resource/pool fodder — the difference is only what consumes the output.</summary>
        private List<IModifierDescriptor> LoadDescriptors(List<ItemModifier> modifiers, string context)
        {
            var result = new List<IModifierDescriptor>();
            foreach (var modifier in modifiers)
            {
                var descriptor = ToDescriptor(modifier, context);
                if (descriptor != null) result.Add(descriptor);
            }

            return result;
        }

        private IModifierDescriptor? ToDescriptor(ItemModifier modifier, string context)
        {
            if (modifier.Parts.Count > 0)
            {
                var parts = new List<IModifierDescriptor>();
                foreach (var part in modifier.Parts)
                {
                    var partDescriptor = ToDescriptor(part, context);
                    if (partDescriptor == null) return null; // a bad part drops the whole composite
                    parts.Add(partDescriptor);
                }

                return new CompositeDescriptor(parts) { Weight = modifier.Weight };
            }

            ModifierValueType type;
            try
            {
                type = ParseModifierType(modifier.ModifierType);
            }
            catch (FormatException exception)
            {
                Tracker.TrackError($"Skipping modifier of '{context}': {exception.Message}");
                return null;
            }

            if (EnumParser.TryParseEnum<EntityParameter>(modifier.Parameter, out var entityParameter))
                return new ParameterDescriptor(entityParameter, type, modifier.Value, EnumParser.ParseEnumOrDefault<ModifierScope>(modifier.Scope)) { Weight = modifier.Weight };

            if (EnumParser.TryParseEnum<ContextParameter>(modifier.Parameter, out var contextParameter))
                return new ContextDescriptor(contextParameter, type, modifier.Value) { Weight = modifier.Weight };

            Tracker.TrackError($"Skipping modifier of '{context}': '{modifier.Parameter}' is neither an EntityParameter nor a ContextParameter");
            return null;
        }

        // Materializes authored lines into fresh entity/context instances, split into buckets for the item's setters.
        private CollectingSink MaterializeLines(List<ItemModifier> lines, string source)
        {
            var sink = new CollectingSink();
            foreach (var descriptor in LoadDescriptors(lines, "item"))
                _materializer.Materialize(descriptor, sink, source);

            return sink;
        }

        private List<IModifier> LoadModifiers(List<ItemModifier> modifiers)
        {
            var result = new List<IModifier>();
            foreach (var m in modifiers)
            {
                if (m.Parts.Count > 0)
                {
                    var composite = LoadCompositeModifier("item", m);
                    if (composite != null) result.Add(composite);
                    continue;
                }

                if (!TryParseModifier("item", m.Parameter, m.ModifierType, out var parameter, out var type)) continue;

                result.Add(new Modifier(type, parameter, m.Value)
                {
                    Scope = EnumParser.ParseEnumOrDefault<ModifierScope>(m.Scope)
                });
            }

            return result;
        }

        /// <summary>One weighted entry granting all its parts at once. A bad part drops the whole entry
        /// (a half-granted composite would be misleading), reported per the usual tolerance rules.</summary>
        private CompositeModifier? LoadCompositeModifier(string context, ItemModifier data)
        {
            var parts = new List<IModifierInstance>();
            foreach (var part in data.Parts)
            {
                if (!TryParseModifier(context, part.Parameter, part.ModifierType, out var parameter, out var type)) return null;

                var instance = ModifiersCreator.CreateModifierInstance(parameter, type, part.Value, context);
                instance.Scope = EnumParser.ParseEnumOrDefault<ModifierScope>(part.Scope);
                parts.Add(instance);
            }

            return parts.Count > 0 ? new CompositeModifier(data.Weight, parts, context) : null;
        }

        private List<IUpgradingResource> LoadUpgradeResources(List<UpgradeResourceData> upgradeResourceData) =>
            upgradeResourceData.Select(upgradeResource =>
            {
                var rarity = EnumParser.ParseEnum<Rarity>(upgradeResource.Rarity);
                var category = EnumParser.ParseEnum<EquipmentCategory>(upgradeResource.Category);
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

                var materialDescriptors = LoadDescriptors(craftingData.Material.Modifiers, craftingData.Id);
                var rarity = EnumParser.ParseEnum<Rarity>(craftingData.Rarity);
                var material = factory.CreateMaterial(materialDescriptors, category);
                items.Add(factory.CreateCraftingResource(craftingData.Id, craftingData.MaxStackSize, craftingData.Tags, material, rarity));
            }

            return items;
        }

        private Dictionary<EquipmentCategory, List<IRequirement>> LoadCategoryRequirements(List<CategoryRequirementsData> categories)
        {
            var result = new Dictionary<EquipmentCategory, List<IRequirement>>();
            foreach (var categoryData in categories)
            {
                var category = EnumParser.ParseEnum<EquipmentCategory>(categoryData.Category);
                result[category] = categoryData.Requirements
                    .Select(requirement => factory.CreateRequirement(EnumParser.ParseEnum<RequirementType>(requirement.Type), requirement.Id, requirement.Amount))
                    .ToList();
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
                parameter = EnumParser.ParseEnum<EntityParameter>(parameterValue);
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
