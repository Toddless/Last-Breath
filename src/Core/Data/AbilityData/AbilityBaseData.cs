namespace Core.Data.AbilityData
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces.Abilities;

    public record AbilityBaseData
    {
        public string Id { get; init; } = string.Empty;
        public string[] Tags { get; init; } = [];
        public int Cooldown { get; init; }
        public int CostValue { get; init; }
        public Costs CostsType { get; init; } = Costs.Mana;
        public float Damage { get; init; }
        public float WeaponDamageScale { get; init; }
        public float SpellDamageScale { get; init; }
        public Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; init; } = [];
    };
}
