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
    using Modifiers;
    using Modifiers.Context;
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
            ["flag"] = ModifierValueType.Flag,
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
                data.RarityMultipliers.ToDictionary(kvp => EnumParser.ParseEnum<Rarity>(kvp.Key), kvp => kvp.Value),
                data.GoldPerBudgetUnit,
                data.MaxGoldPerKill);
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

        /// <summary>Rollable pools (item/family pools, mythic pool, additive recraft pools) parse straight
        /// into descriptors: every entry MUST claim a slot family (see <see cref="AffixPolicy.Required"/>).</summary>
        public Dictionary<string, List<IModifierDescriptor>> ParseEquipItemModifierPools(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipModifiersPoolRoot>(json, s_settings) ?? throw new InvalidOperationException();
            var pools = new Dictionary<string, List<IModifierDescriptor>>();
            foreach (var modifierPool in data.Root)
                pools.TryAdd(modifierPool.Id, LoadDescriptors(modifierPool.ModifiersPool, modifierPool.Id, AffixPolicy.Required));

            return pools;
        }

        public List<IItem> ParseItems(string json)
        {
            var data = JsonConvert.DeserializeObject<ItemDataList>(json, s_settings);
            return (data?.Items ?? [])
                .Select(item => factory.CreateItem(item.Id, EnumParser.ParseEnum<Rarity>(item.Rarity), item.MaxStackSize, item.Tags))
                .ToList();
        }

        /// <summary>Equip templates parse into BLUEPRINTS, not items: authored lines stay descriptors
        /// (ranges unrolled) and no domain object is created here — <see cref="Items.IEquipItemMinter"/>
        /// owns materialization, so parsing never consumes a roll.</summary>
        public List<EquipItemBlueprint> ParseEquipItems(string json)
        {
            var data = JsonConvert.DeserializeObject<EquipItemDataList>(json, s_settings);
            var blueprints = new List<EquipItemBlueprint>();

            foreach (var item in data?.Items ?? [])
            {
                // An invalid level spec poisons the whole item (its lines would land mis-scaled), so the
                // item is skipped entirely — unlike a bad modifier line, which only drops itself.
                if (!IsValidUpdateLevel(item)) continue;

                var piece = EnumParser.ParseEnum<EquipmentPiece>(item.EquipmentPart);
                blueprints.Add(new EquipItemBlueprint
                {
                    Id = item.Id,
                    Piece = piece,
                    Weapon = piece == EquipmentPiece.Weapon ? ParseWeaponBlock(item) : null,
                    Rarity = EnumParser.ParseEnum<Rarity>(item.Rarity),
                    Tags = item.Tags,
                    BasePrice = item.BasePrice,
                    UpdateLevel = item.UpdateLevel,
                    MaxUpdateLevel = item.MaxUpdateLevel,
                    BaseStats = ParseBaseStats(item),
                    // Authored lines never carry an affix — they are implicit by definition, not rolled slots.
                    Implicits = LoadDescriptors(item.Implicits, item.Id, AffixPolicy.Forbidden),
                    Modifiers = LoadDescriptors(item.Modifiers, item.Id, AffixPolicy.Forbidden),
                    Grants = LoadGrantBlueprints(item),
                });
            }

            return blueprints;
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
                    recipeData.UnlockAtMastery,
                    recipeData.BasePrice,
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
                var descriptors = LoadCategorizedDescriptors(categoryData.Modifiers, categoryData.ByCategory, categoryData.Id, AffixPolicy.Required);
                categoryDic[categoryData.Id] = factory.CreateMaterialCategory(descriptors, categoryData.Id);
            }

            var upgrades = LoadUpgradeResources(data?.UpgradeResources ?? []);
            var craftingResources = LoadCraftingResources(categoryDic, data?.CraftingResources ?? []);

            var items = new List<IItem>();
            items.AddRange(upgrades.Cast<IItem>());
            items.AddRange(craftingResources.Cast<IItem>());

            return items;
        }

        public Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<CostRequirement>>> ParseUpgradeCosts(string json)
        {
            var data = JsonConvert.DeserializeObject<UpgradeCostsData>(json, s_settings) ?? throw new InvalidOperationException();
            return new Dictionary<CraftingMode, Dictionary<EquipmentCategory, List<CostRequirement>>>
            {
                [CraftingMode.Upgrade] = LoadCategoryCosts(data.Upgrade, "upgrade"),
                [CraftingMode.Recraft] = LoadCategoryCosts(data.Recraft, "recraft"),
                [CraftingMode.Ascend] = LoadCategoryCosts(data.Ascend, "ascend")
            };
        }

        // Grant modifier lines are parsed once into shared templates; the factory mints fresh
        // instances per grant at mint time, so blueprints stay immutable.
        private List<GrantBlueprint> LoadGrantBlueprints(EquipItemData itemData)
        {
            var grants = new List<GrantBlueprint>();
            foreach (var grantData in itemData.Grants)
            {
                var kind = EnumParser.ParseEnum<GrantKind>(grantData.Kind);
                grants.Add(new GrantBlueprint(kind, grantData.Id, LoadModifiers(grantData.Modifiers), grantData.Properties));
            }

            return grants;
        }

        private static bool IsValidUpdateLevel(EquipItemData item)
        {
            var level = item.UpdateLevel;
            if (!level.IsMalformed && level.Min >= 0 && level.Max >= level.Min && level.Max <= item.MaxUpdateLevel) return true;

            Tracker.TrackError($"Skipping item '{item.Id}': updateLevel [{level.Min}..{level.Max}] is invalid (bounds must be 0 <= min <= max <= maxUpdateLevel {item.MaxUpdateLevel})");
            return false;
        }

        /// <summary>Base stats parse per entry with the usual tolerance: a typo'd parameter or a
        /// broken range drops that stat with a report, never the item.</summary>
        private static List<BaseStatBlueprint> ParseBaseStats(EquipItemData item)
        {
            var stats = new List<BaseStatBlueprint>();
            foreach (var stat in item.BaseStats)
            {
                if (!EnumParser.TryParseEnum<EntityParameter>(stat.Parameter, out var parameter))
                {
                    Tracker.TrackError($"Skipping base stat of '{item.Id}': '{stat.Parameter}' is not an EntityParameter");
                    continue;
                }

                if (!stat.Value.IsValid)
                {
                    Tracker.TrackError($"Skipping base stat of '{item.Id}': value [{stat.Value.Min}..{stat.Value.Max}] is not a number or a valid min/max range");
                    continue;
                }

                stats.Add(new BaseStatBlueprint(parameter, new ValueRange(stat.Value.Min, stat.Value.Max)));
            }

            return stats;
        }

        private static WeaponBlueprint ParseWeaponBlock(EquipItemData item) => new(
            EnumParser.ParseEnum<WeaponType>(item.WeaponType),
            EnumParser.ParseEnum<Handedness>(item.Handedness),
            item.Damage,
            item.CritChance,
            item.CritDamage);

        /// <summary>Where a modifier line may claim a slot family. Authored item lines and composite parts
        /// must not (they never enter an affix roll); rollable pool entries MUST (an unmarked entry could
        /// never occupy a slot, so it is a data error, not a silent None).</summary>
        private enum AffixPolicy
        {
            Forbidden,
            Required,
        }

        /// <summary>Item and material lines share one "parameter" namespace: a name resolves as
        /// <see cref="EntityParameter"/> first, then as <see cref="ContextParameter"/> (pipeline knobs).
        /// Unknown in both → the entry is reported and dropped, per the usual tolerance rules. One parser feeds
        /// both authored item lines and resource/pool fodder — the difference is only the affix policy.</summary>
        private List<IModifierDescriptor> LoadDescriptors(List<ItemModifier> modifiers, string context, AffixPolicy policy, EquipmentCategory? onlyFor = null)
        {
            var result = new List<IModifierDescriptor>();
            foreach (var modifier in modifiers)
            {
                var descriptor = ToDescriptor(modifier, context, policy, onlyFor);
                if (descriptor != null) result.Add(descriptor);
            }

            return result;
        }

        /// <summary>The flat list serves every equipment category; each byCategory section stamps its
        /// entries with the section's category (the same ore gives armor to a cuirass and damage to a
        /// blade). A section under an unknown category key is dropped loudly, the rest still load.</summary>
        private List<IModifierDescriptor> LoadCategorizedDescriptors(
            List<ItemModifier> flat,
            Dictionary<string, List<ItemModifier>>? byCategory,
            string context,
            AffixPolicy policy)
        {
            var result = LoadDescriptors(flat, context, policy);
            if (byCategory == null) return result;

            foreach ((string key, var entries) in byCategory)
            {
                if (!EnumParser.TryParseEnum<EquipmentCategory>(key, out var category))
                {
                    Tracker.TrackError($"Skipping byCategory section '{key}' of '{context}': not an {nameof(EquipmentCategory)}");
                    continue;
                }

                result.AddRange(LoadDescriptors(entries, context, policy, category));
            }

            return result;
        }

        private IModifierDescriptor? ToDescriptor(ItemModifier modifier, string context, AffixPolicy policy, EquipmentCategory? onlyFor = null)
        {
            if (!TryParseAffix(modifier, context, policy, out var affix)) return null;

            // "+Min..Max sharpening levels" — an item operation only the ascension gift applies.
            if (modifier.ExtraUpgradeLevels is { } levels)
            {
                if (levels is { IsMalformed: false, Min: >= 1 } && levels.Max >= levels.Min)
                {
                    return new UpgradeLevelsDescriptor(levels.Min, levels.Max) { Weight = modifier.Weight, Affix = affix, OnlyFor = onlyFor };
                }

                Tracker.TrackError($"Skipping modifier of '{context}': invalid extraUpgradeLevels range");
                return null;

            }

            // Rollable grant: behaviour a line cannot express (per-turn state, triggers) enters the pool as
            // the passive/effect it really is. The kind is parsed strictly — a typo is an error, not a default.
            if (modifier.Grant is { } grantData)
            {
                if (grantData.Id.Length == 0)
                {
                    Tracker.TrackError($"Skipping modifier of '{context}': a grant entry needs an id");
                    return null;
                }

                if (!EnumParser.TryParseEnum<GrantKind>(grantData.Kind, out var grantKind))
                {
                    Tracker.TrackError($"Skipping modifier of '{context}': '{grantData.Kind}' is not a grant kind");
                    return null;
                }

                return new GrantDescriptor(grantKind, grantData.Id, grantData.Properties) { Weight = modifier.Weight, Affix = affix, OnlyFor = onlyFor };
            }

            if (modifier.Parts.Count > 0)
            {
                var parts = new List<IModifierDescriptor>();
                foreach (var part in modifier.Parts)
                {
                    // The affix lives on the composite root only — a part carrying its own is a data error.
                    var partDescriptor = ToDescriptor(part, context, AffixPolicy.Forbidden);
                    if (partDescriptor == null) return null; // a bad part drops the whole composite
                    parts.Add(partDescriptor);
                }

                return new CompositeDescriptor(parts) { Weight = modifier.Weight, Affix = affix, OnlyFor = onlyFor };
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

            // A flag line is a switch, not a number: the data writes no value at all and the pinned 1 never
            // rolls and never scales. Every other type must bring its value (or spread) as usual.
            ValueRange range = new(1f, 1f);
            if (type != ModifierValueType.Flag && !TryParseRange(modifier, context, out range)) return null;

            if (EnumParser.TryParseEnum<EntityParameter>(modifier.Parameter, out var entityParameter))
            {
                if (type == ModifierValueType.Flag)
                {
                    Tracker.TrackError($"Skipping modifier of '{context}': '{modifier.Parameter}' is an entity parameter — a flag has no meaning in parameter math");
                    return null;
                }

                return new ParameterDescriptor(entityParameter, type, range, EnumParser.ParseEnumOrDefault<ModifierScope>(modifier.Scope)) { Weight = modifier.Weight, Affix = affix, OnlyFor = onlyFor };
            }

            if (EnumParser.TryParseEnum<ContextParameter>(modifier.Parameter, out var contextParameter))
            {
                // The knob, not the entry, decides whether it takes a line at all and whether that line is a
                // switch or an amount. A knob with no binding throws the moment the item is equipped, far
                // from the pool that named it; a flag written on a knob that reads a value never rolls and
                // never scales — it reaches the battle pipeline as the pinned 1, which is the whole effect
                // handed out for free.
                if (ContextKnobs.WhyRefused(contextParameter, type) is { } refusal)
                {
                    Tracker.TrackError($"Skipping modifier of '{context}': {refusal}");
                    return null;
                }

                return new ContextDescriptor(contextParameter, type, range) { Weight = modifier.Weight, Affix = affix, OnlyFor = onlyFor };
            }

            Tracker.TrackError($"Skipping modifier of '{context}': '{modifier.Parameter}' is neither an EntityParameter nor a ContextParameter");
            return null;
        }

        /// <summary>Strict affix policy: a typo never falls back silently. Lines that can never enter an
        /// affix roll (authored implicits/modifiers, composite parts) must not claim a slot family at all,
        /// while rollable pool entries must claim exactly one — a missing or None affix there is an error.</summary>
        private static bool TryParseAffix(ItemModifier modifier, string context, AffixPolicy policy, out AffixKind affix)
        {
            affix = AffixKind.None;
            if (string.IsNullOrEmpty(modifier.Affix))
            {
                if (policy == AffixPolicy.Forbidden) return true;

                Tracker.TrackError($"Skipping modifier of '{context}': a rollable pool entry must declare an affix (Prefix or Suffix)");
                return false;
            }

            if (policy == AffixPolicy.Forbidden)
            {
                Tracker.TrackError($"Skipping modifier of '{context}': affix '{modifier.Affix}' is not allowed on authored item lines or composite parts");
                return false;
            }

            if (!EnumParser.TryParseEnum(modifier.Affix, out affix))
            {
                Tracker.TrackError($"Skipping modifier of '{context}': '{modifier.Affix}' is not a valid {nameof(AffixKind)}");
                return false;
            }

            if (affix == AffixKind.None)
            {
                Tracker.TrackError($"Skipping modifier of '{context}': None is not a slot family — pool entries must be Prefix or Suffix");
                return false;
            }

            return true;
        }

        private static bool TryParseRange(ItemModifier modifier, string context, out ValueRange range)
        {
            range = default;
            if (!modifier.Value.IsValid)
            {
                Tracker.TrackError($"Skipping modifier of '{context}': value [{modifier.Value.Min}..{modifier.Value.Max}] is not a number or a valid min/max range");
                return false;
            }

            range = new ValueRange(modifier.Value.Min, modifier.Value.Max);
            return true;
        }

        /// <summary>IModifier pool/grant lines predate ranges: they carry one float until the descriptor
        /// migration (next block), so a ranged value there is reported and dropped.</summary>
        private static bool TryGetFixedValue(ItemModifier modifier, string context, out float value)
        {
            value = 0f;
            if (!modifier.Value.IsValid || !modifier.Value.IsFixed)
            {
                Tracker.TrackError($"Skipping modifier of '{context}': expected a single number, got [{modifier.Value.Min}..{modifier.Value.Max}]");
                return false;
            }

            value = modifier.Value.Min;
            return true;
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
                if (!TryGetFixedValue(m, "item", out float value)) continue;

                result.Add(new Modifier(type, parameter, value)
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
                if (!TryGetFixedValue(part, context, out float value)) return null;

                var instance = ModifiersCreator.CreateModifierInstance(parameter, type, value, context);
                instance.Scope = EnumParser.ParseEnumOrDefault<ModifierScope>(part.Scope);
                parts.Add(instance);
            }

            return parts.Count > 0 ? new CompositeModifier(data.Weight, parts, context) : null;
        }

        private List<IUpgradingResource> LoadUpgradeResources(List<UpgradeResourceData> upgradeResourceData) =>
            upgradeResourceData.Select(upgradeResource =>
            {
                var rarity = EnumParser.ParseEnum<Rarity>(upgradeResource.Rarity);
                // Category is optional: dusts and sharpening runes serve one equipment category,
                // fluxes and creation runes serve any and simply omit the field. A PRESENT but
                // invalid value still fails the strict parse.
                EquipmentCategory? category = string.IsNullOrEmpty(upgradeResource.Category)
                    ? null
                    : EnumParser.ParseEnum<EquipmentCategory>(upgradeResource.Category);
                var resource = factory.CreateUpgradeResource(upgradeResource.Id, upgradeResource.Tags, rarity, category, upgradeResource.MaxStackSize);
                // The factory signature predates pricing; the parser stamps the authored base directly.
                if (resource is UpgradeResource priced) priced.BasePrice = upgradeResource.BasePrice;
                return resource;
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

                var materialDescriptors = LoadCategorizedDescriptors(craftingData.Material.Modifiers, craftingData.Material.ByCategory, craftingData.Id, AffixPolicy.Required);
                var rarity = EnumParser.ParseEnum<Rarity>(craftingData.Rarity);
                var material = factory.CreateMaterial(materialDescriptors, category);
                var resource = factory.CreateCraftingResource(craftingData.Id, craftingData.MaxStackSize, craftingData.Tags, material, rarity);
                // The factory signature predates pricing; the parser stamps the authored base directly.
                if (resource is CraftingResource priced) priced.BasePrice = craftingData.BasePrice;
                items.Add(resource);
            }

            return items;
        }

        private static Dictionary<EquipmentCategory, List<CostRequirement>> LoadCategoryCosts(List<CategoryRequirementsData> categories, string section)
        {
            var result = new Dictionary<EquipmentCategory, List<CostRequirement>>();
            foreach (var categoryData in categories)
            {
                var category = EnumParser.ParseEnum<EquipmentCategory>(categoryData.Category);
                var requirements = new List<CostRequirement>();
                foreach (var requirementData in categoryData.Requirements)
                {
                    var requirement = ToCostRequirement(requirementData, $"{section}/{category}");
                    if (requirement != null) requirements.Add(requirement);
                }

                result[category] = requirements;
            }

            return result;
        }

        /// <summary>Strict per-entry parse of one cost line: a bad type, an unknown rarity key or an
        /// override that resolves to no id / non-positive amount drops the WHOLE line (a half-parsed
        /// cost would silently sell the operation short), reported per the usual tolerance rules.</summary>
        private static CostRequirement? ToCostRequirement(UpgradeRequirementData data, string context)
        {
            if (!EnumParser.TryParseEnum<RequirementType>(data.Type, out var type))
            {
                Tracker.TrackError($"Skipping cost line of '{context}': '{data.Type}' is not a valid {nameof(RequirementType)}");
                return null;
            }

            Dictionary<Rarity, RarityCostOverride>? byRarity = null;
            foreach ((string rarityKey, var overrideData) in data.ByRarity ?? [])
            {
                if (!EnumParser.TryParseEnum<Rarity>(rarityKey, out var rarity))
                {
                    Tracker.TrackError($"Skipping cost line of '{context}': '{rarityKey}' is not a valid {nameof(Rarity)}");
                    return null;
                }

                string? effectiveId = overrideData.Id ?? data.Id;
                int? effectiveAmount = overrideData.Amount ?? data.Amount;
                if (string.IsNullOrEmpty(effectiveId) || effectiveAmount is not > 0)
                {
                    Tracker.TrackError($"Skipping cost line of '{context}': the '{rarityKey}' override resolves to id '{effectiveId}' x{effectiveAmount} (needs a non-empty id and a positive amount)");
                    return null;
                }

                (byRarity ??= [])[rarity] = new RarityCostOverride(overrideData.Id, overrideData.Amount);
            }

            // A line must be resolvable somewhere: complete defaults, or at least one rarity override.
            if (byRarity == null && (string.IsNullOrEmpty(data.Id) || data.Amount is not > 0))
            {
                Tracker.TrackError($"Skipping cost line of '{context}': no byRarity entries and the defaults are incomplete (id '{data.Id}' x{data.Amount})");
                return null;
            }

            return new CostRequirement(type, data.Id, data.Amount, byRarity);
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
