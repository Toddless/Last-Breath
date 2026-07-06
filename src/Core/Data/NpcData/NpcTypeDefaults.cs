namespace Core.Data.NpcData
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// Per-EntityType spawn defaults agreed with design. Enum members are the source of truth,
    /// display names map as: Regular=Обычный, Special=Редкий, Elit=Элитный, Unique=Специальный.
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

        /// <summary>Parameters that grow with level; chances and multipliers stay flat.</summary>
        private static readonly HashSet<EntityParameter> s_levelScaled =
        [
            EntityParameter.Health,
            EntityParameter.Barrier,
            EntityParameter.Mana,
            EntityParameter.Damage,
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

        public static int DefaultAbilityCount(EntityType type) => s_abilityCount.GetValueOrDefault(type, 2);

        public static bool ScalesWithLevel(EntityParameter parameter) => s_levelScaled.Contains(parameter);
    }
}
