namespace Core.Context
{
    using Battle;
    using Entity;
    using Enums;
    using Godot;

    public class AttackContext(IFightable attacker, IFightable target, float baseDamage, RandomNumberGenerator rnd, IAttackContextScheduler attackContextScheduler)
        : IAttackContext
    {
        public IAttackContextScheduler AttackContextScheduler { get; } = attackContextScheduler;
        public RandomNumberGenerator Rnd { get; } = rnd;
        public IFightable Attacker { get; } = attacker;
        public IFightable Target { get; } = target;
        public float BaseDamage { get; } = baseDamage;
        public AttackResults Result { get; set; }
        public float RawCriticalChance { get; set; } = attacker.Parameters.CriticalChance;
        // Default from the attacker like RawCriticalChance/RawAccuracy: several attack paths
        // (basic attacks via BattleArena, reactions via CreateReaction, SoA) set the chance but
        // forgot the damage, leaving it 0 — every crit from them dealt FinalDamage × 0 = 0.
        public float RawCriticalDamage { get; set; } = attacker.Parameters.CriticalDamage;
        public float RawAccuracy { get; set; } = attacker.Parameters.Accuracy;
        public float AdditionalDamage { get; set; }
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
    }
}
