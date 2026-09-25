namespace Core.Data.NpcModifiersData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>The seven sections of the npc modifier file, one per kind of modifier. The key of a
    /// section IS the kind: it picks the shape its entries are read in and becomes the group uniqueness is
    /// weighed within, so a section renamed is a kind nothing creates. Each is read under its own constant
    /// rather than under whatever a naming strategy makes of the member — a member renamed would otherwise
    /// move the key and leave the constant behind, in silence.</summary>
    public record ModifiersData
    {
        /// <summary>Json key of the section holding the modifiers that strengthen the other modifiers.</summary>
        public const string ScaleSection = "scale";

        /// <summary>Json key of the section holding the modifiers that push a drop up the tiers.</summary>
        public const string TierUpgradeSection = "tierUpgrade";

        /// <summary>Json key of the section holding the modifiers that promise items outright.</summary>
        public const string GuaranteedItemsSection = "guaranteedItems";

        /// <summary>Json key of the section holding the modifiers that weight the tier chances.</summary>
        public const string TierMultiplierSection = "tierMultiplier";

        /// <summary>Json key of the section holding the modifiers that grant an effect to every drop.</summary>
        public const string ItemEffectsSection = "itemEffects";

        /// <summary>Json key of the section holding the modifiers that weight the rarity chances.</summary>
        public const string RarityUpgradeSection = "rarityUpgrade";

        /// <summary>Json key of the section holding the modifiers that floor a drop's rarity.</summary>
        public const string MinRaritySection = "minRarity";

        [JsonProperty(ScaleSection)] public List<ScaleModifierData> Scale { get; init; } = [];
        [JsonProperty(TierUpgradeSection)] public List<TierUpgradeData> TierUpgrade { get; init; } = [];
        [JsonProperty(GuaranteedItemsSection)] public List<GuaranteedItemsData> GuaranteedItems { get; init; } = [];
        [JsonProperty(TierMultiplierSection)] public List<TierMultiplierData> TierMultiplier { get; init; } = [];
        [JsonProperty(ItemEffectsSection)] public List<ItemEffectData> ItemEffects { get; init; } = [];
        [JsonProperty(RarityUpgradeSection)] public List<RarityUpgradeModifierData> RarityUpgrade { get; init; } = [];
        [JsonProperty(MinRaritySection)] public List<MinRarityModifierData> MinRarity { get; init; } = [];

        /// <summary>The keys a section may be written under, read off the one map that joins a key to the
        /// shape beneath it: a section added there is a section this answer knows about.</summary>
        public static IReadOnlyCollection<string> SectionKeys { get; } = new ModifiersData().Sections().Keys;

        /// <summary>The entries of every section, keyed by the section that wrote them — the one place the
        /// key and the shape it names are joined, and the order the file writes them in.</summary>
        public Dictionary<string, List<NpcModifierData>> Sections() => new()
        {
            [ScaleSection] = [.. Scale],
            [TierUpgradeSection] = [.. TierUpgrade],
            [GuaranteedItemsSection] = [.. GuaranteedItems],
            [TierMultiplierSection] = [.. TierMultiplier],
            [ItemEffectsSection] = [.. ItemEffects],
            [RarityUpgradeSection] = [.. RarityUpgrade],
            [MinRaritySection] = [.. MinRarity],
        };
    }
}
