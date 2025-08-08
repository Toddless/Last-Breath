namespace Core.Constants
{
    using System.Collections.Generic;
    using Core.Enums;

    public static class TagConstants
    {
        #region Fabric
        public const string Cloth = "cloth";
        public const string Linen = "linen";
        public const string Wool = "wool";
        public const string Silk = "silk";
        public const string Velvet = "velvet";
        #endregion

        #region Metal / Steel
        public const string OreIron = "ore_iron";
        public const string IronIngot = "iron_ingot";
        public const string SteelIngot = "steel_ingot";
        public const string CopperIngot = "copper_ingot";
        public const string BronzeIngot = "bronze_ingot";
        public const string SilverIngot = "silver_ingot";
        public const string GoldIngot = "gold_ingot";
        public const string Mithril = "mithril";
        public const string Adamantine = "adamantine";
        public const string ColdIron = "cold_iron";
        #endregion

        #region Leather
        public const string RawHide = "raw_hide";
        public const string Leather = "leather";
        public const string CuredLeather = "cured_leather";
        public const string ReinforcedLeather = "reinforced_leather";
        public const string DragonHide = "dragon_hide";
        public const string Fur = "fur";
        public const string Chitin = "chitin";
        #endregion

        #region Gems
        public const string Ruby = "ruby";
        public const string Sapphire = "sapphire";
        public const string Emerald = "emerald";
        public const string Topaz = "topaz";
        public const string Diamond = "diamond";
        public const string Amethyst = "amethyst";
        public const string Moonstone = "moonstone";
        public const string Opal = "opal";
        public const string Bloodstone = "bloodstone";
        #endregion

        // TODO: Refill
        #region Essences
        public const string EssenceFire = "essence_fire";
        public const string EssenceWater = "essence_water";
        public const string EssenceEarth = "essence_earth";
        public const string EssenceAir = "essence_air";
        public const string EssenceArcane = "essence_arcane";
        public const string EssenceLife = "essence_life";
        public const string EssenceDeath = "essence_death";
        public const string EssenceLight = "essence_light";
        public const string EssenceShadow = "essence_shadow";
        public const string EssenceVoid = "essence_void";
        public const string SoulFragment = "soul_fragment";
        #endregion

        #region Common
        public const string Raw = "raw";
        public const string Refined = "refined";
        public const string Ore = "ore";
        public const string Ingot = "ingot";
        public const string Plate = "plate";
        public const string Catalyst = "catalyst";
        public const string BindingAgent = "binding_agent";
        public const string CraftingFuel = "crafting_fuel";
        public const string ToolPart = "tool_part";
        public const string Enchanted = "enchanted";
        public const string Cursed = "cursed";
        public const string Common = "common";
        public const string Uncommon = "uncommon";
        public const string Rare = "rare";
        public const string Epic = "epic";
        public const string Legendary = "legendary";
        public const string Mythic = "mythic";
        #endregion

        public static readonly string[] AllRarityTags = [Common, Uncommon, Rare, Epic, Legendary, Mythic];

        public static readonly IReadOnlyDictionary<ResourceCategory, string[]> DefaultTagsByCategory =
            new Dictionary<ResourceCategory, string[]>
            {
                { ResourceCategory.Fabric, new[]{ Cloth, Linen, Wool, Silk, Velvet } },
                { ResourceCategory.Steel, new[]{ IronIngot, SteelIngot, CopperIngot, BronzeIngot, SilverIngot, GoldIngot, Mithril, Adamantine } },
                { ResourceCategory.Leather, new[]{ RawHide, Leather, CuredLeather, ReinforcedLeather, DragonHide, Fur, Chitin } },
                { ResourceCategory.Jewel, new[]{ Ruby, Sapphire, Emerald, Topaz, Diamond, Amethyst, Moonstone, Opal, Bloodstone } },
                { ResourceCategory.Essence, new[]{ EssenceFire, EssenceWater, EssenceEarth, EssenceAir, EssenceArcane, EssenceLife, EssenceDeath, EssenceLight, EssenceShadow, EssenceVoid, SoulFragment } }
            };

        public static string Normalize(string tag) => (tag ?? string.Empty).Trim().ToLowerInvariant();

        public static bool HasTag(IEnumerable<string> tags, string tag)
        {
            if (tags == null) return false;
            var normalized = Normalize(tag);
            foreach (var t in tags)
                if (Normalize(t) == normalized) return true;
            return false;
        }

        public static IEnumerable<string> GetDefaultTags(ResourceCategory category)
        {
            if (DefaultTagsByCategory.TryGetValue(category, out var tags)) return tags;
            return [];
        }

        public static string RarityToTag(Rarity rarity) => rarity.ToString().ToLowerInvariant();
    }
}
