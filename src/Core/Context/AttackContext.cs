namespace Core.Context
{
    using System.Collections.Generic;
    using System.Linq;
    using Battle;
    using Entity;
    using Enums;
    using Godot;

    public class AttackContext(IFightable attacker, IFightable target, float baseDamage, RandomNumberGenerator rnd, IAttackContextScheduler attackContextScheduler)
        : IAttackContext
    {
        // Flat elemental damage parameters ride along with every attack (weapon prefixes "+X fire damage").
        private static readonly (EntityParameter Parameter, DamageType Element)[] s_attackElements =
        [
            (EntityParameter.FireDamage, DamageType.Fire),
            (EntityParameter.ColdDamage, DamageType.Cold),
            (EntityParameter.LightningDamage, DamageType.Lightning)
        ];

        private readonly Dictionary<DamageType, float> _damageComponents = SeedComponents(attacker, baseDamage);

        public IAttackContextScheduler AttackContextScheduler { get; } = attackContextScheduler;
        public RandomNumberGenerator Rnd { get; } = rnd;
        public IFightable Attacker { get; } = attacker;
        public IFightable Target { get; } = target;
        public float BaseDamage { get; } = baseDamage;
        public IReadOnlyDictionary<DamageType, float> DamageComponents => _damageComponents;
        public float TotalDamage => _damageComponents.Values.Sum();
        public AttackResults Result { get; set; }
        public float RawCriticalChance { get; set; } = attacker.Parameters.CriticalChance;
        // Default from the attacker like RawCriticalChance/RawAccuracy: several attack paths
        // (basic attacks via BattleArena, reactions via CreateReaction, SoA) set the chance but
        // forgot the damage, leaving it 0 — every crit from them dealt FinalDamage × 0 = 0.
        public float RawCriticalDamage { get; set; } = attacker.Parameters.CriticalDamage;
        public float RawAccuracy { get; set; } = attacker.Parameters.Accuracy;
        public float FinalDamage { get; set; }
        public bool IsCritical { get; set; }
        // Необходимо как-то убедиться, что единожды выставленный чек не будет впоследствии изменен.
        public bool ForceCriticalAttack { get; set; }
        // NOTE: Модификаторы, делающие атаки неблокируемыми/неизбежными, должны иметь Высокий/Абсолютный приоритет
        public bool IsUnevadable { get; set; }
        public bool IsUnblockable { get; set; }
        public string? SourceAbilityId { get; set; }
        public int Index { get; set; }
        public int TotalCount { get; set; } = 1;
        public bool IsFirst => Index == 0;
        public bool IsLast => Index == TotalCount - 1;
        public int ReactionDepth { get; private init; }

        public bool IsValid => Target.IsAlive && Attacker.IsAlive;

        public void AddDamage(DamageType type, float amount) => _damageComponents[type] = _damageComponents.GetValueOrDefault(type, 0f) + amount;

        public void SetDamage(DamageType type, float amount) => _damageComponents[type] = amount;

        public void ScaleDamage(float factor)
        {
            foreach (DamageType type in _damageComponents.Keys.ToArray())
                _damageComponents[type] *= factor;
        }

        public IAttackContext CreateReaction(IFightable attacker, IFightable target, float baseDamage) =>
            new AttackContext(attacker, target, baseDamage, Rnd, AttackContextScheduler)
            {
                ReactionDepth = ReactionDepth + 1,
                RawCriticalChance = attacker.Parameters.CriticalChance
            };

        public bool Schedule()
        {
            if (!Attacker.IsAlive || !Target.IsAlive) return false;
            AttackContextScheduler.Schedule(this);
            return true;
        }

        private static Dictionary<DamageType, float> SeedComponents(IFightable attacker, float baseDamage)
        {
            var components = new Dictionary<DamageType, float> { [DamageType.Physical] = baseDamage };
            foreach ((EntityParameter parameter, DamageType element) in s_attackElements)
            {
                float value = attacker.Parameters.GetValueForParameter(parameter);
                if (value > 0) components[element] = value;
            }

            return components;
        }
    }
}
