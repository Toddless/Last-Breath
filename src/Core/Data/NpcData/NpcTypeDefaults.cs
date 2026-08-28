namespace Core.Data.NpcData
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// Per-EntityType spawn defaults agreed with design. Enum members are the source of truth,
    /// display names map as: Regular=Common, Special=Rare, Elit=Elit, Unique=Unique.
    /// Boss and Archon are hand-authored, never rolled by spawners.
    /// </summary>
    public static class NpcTypeDefaults
    {
        /// <summary>Take every ability of the stance pool instead of a limited pick.</summary>
        public const int AllAbilities = int.MaxValue;

        private static readonly Dictionary<EntityType, int> s_maxLevel = new()
        {
            [EntityType.Regular] = 15,
            [EntityType.Special] = 25,
            [EntityType.Elit] = 35,
            [EntityType.Unique] = 50,
            [EntityType.Boss] = 80,
            [EntityType.Archon] = 150,
        };

        private static readonly Dictionary<EntityType, int> s_abilityCount = new()
        {
            [EntityType.Regular] = 2,
            [EntityType.Special] = 3,
            [EntityType.Elit] = 4,
            [EntityType.Unique] = 5,
            [EntityType.Boss] = AllAbilities,
            [EntityType.Archon] = AllAbilities,
        };

        /// <summary>Base NPC-modifier count by type; rarity adds on top (см. RarityModifierBonus).</summary>
        private static readonly Dictionary<EntityType, int> s_modifierCount = new()
        {
            [EntityType.Regular] = 1,
            [EntityType.Special] = 2,
            [EntityType.Elit] = 3,
            [EntityType.Unique] = 4,
            [EntityType.Boss] = 5,
            [EntityType.Archon] = 6,
        };

        private static readonly Dictionary<Rarity, int> s_rarityModifierBonus = new()
        {
            [Rarity.Epic] = 1,
            [Rarity.Legendary] = 1,
            [Rarity.Mythic] = 2,
            [Rarity.Unique] = 2,
        };

        /// <summary>Parameters that grow with level; chances and multipliers stay flat.</summary>
        private static readonly HashSet<EntityParameter> s_levelScaled =
        [
            EntityParameter.Health,
            EntityParameter.Barrier,
            EntityParameter.Mana,
            EntityParameter.PhysicalDamage,
            EntityParameter.SpellDamage,
            EntityParameter.Armor,
            EntityParameter.Accuracy,
            EntityParameter.Evade,
            EntityParameter.Strength,
            EntityParameter.Dexterity,
            EntityParameter.Intelligence,
            EntityParameter.HealthRecovery,
            EntityParameter.ManaRecovery,
        ];

        public static int MaxLevel(EntityType type) => s_maxLevel.GetValueOrDefault(type, 15);

        /// <summary>How many ability SLOTS a spawn is offered — a ceiling on the same terms as
        /// <see cref="ModifierCount"/>, with <see cref="AllAbilities"/> as the boss exception that rolls
        /// no dice at all.</summary>
        public static int DefaultAbilityCount(EntityType type) => s_abilityCount.GetValueOrDefault(type, 2);

        /// <summary>How many modifier SLOTS a spawn is offered = base by type + rarity bonus (согласованная
        /// таблица «тип × редкость»). Since issue #223 this is a ceiling and not a promise: each slot is then
        /// offered to the falling chances of the NpcSpawnRolls catalog, so most spawns come out under it.</summary>
        public static int ModifierCount(EntityType type, Rarity rarity) =>
            s_modifierCount.GetValueOrDefault(type, 1) + s_rarityModifierBonus.GetValueOrDefault(rarity, 0);

        public static bool ScalesWithLevel(EntityParameter parameter) => s_levelScaled.Contains(parameter);
    }
}
