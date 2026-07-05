namespace Core.Data.AbilityData
{
    using System.Collections.Generic;
    using Enums;
    using Newtonsoft.Json;

    public record AbilityBaseData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("tags")] public string[] Tags { get; init; } = [];
        [JsonProperty("cooldown")] public int Cooldown { get; init; }
        [JsonProperty("costValue")] public int CostValue { get; init; }
        [JsonProperty("costType")] public Costs CostsType { get; init; } = Costs.Mana;
        [JsonProperty("stance")] public Stance Stance { get; init; } = Stance.Dexterity;
        /// <summary>How the ability picks targets; drives the targeting strategy.</summary>
        [JsonProperty("targetType")] public AbilityTargetType TargetType { get; init; } = AbilityTargetType.Enemy;
        /// <summary>Target cap for the <c>Few*</c> modes; ignored by single-target/self.</summary>
        [JsonProperty("maxTargets")] public int MaxTargets { get; init; } = 1;
        /// <summary>The mastery level at which the ability becomes learnable.</summary>
        [JsonProperty("masteryLevel")] public int MasteryLevel { get; init; } = 1;
        [JsonProperty("damage")] public float Damage { get; init; }
        [JsonProperty("weaponDamageScale")] public float WeaponDamageScale { get; init; }
        [JsonProperty("spellDamageScale")] public float SpellDamageScale { get; init; }
        [JsonProperty("abilityProperties")] public Dictionary<string, float> AbilityProperties { get; init; } = [];
        [JsonProperty("upgrades")] public List<AbilityUpgradeData> Upgrades { get; init; } = [];
    };
}
