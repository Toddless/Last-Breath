namespace Battle.Source
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Godot;

    public class AttackContext(IEntity attacker, IEntity target, float baseDamage, RandomNumberGenerator rnd, IAttackContextScheduler attackContextScheduler)
        : IAttackContext
    {
        public IAttackContextScheduler AttackContextScheduler { get; } = attackContextScheduler;
        public RandomNumberGenerator Rnd { get; } = rnd;
        public IEntity Attacker { get; } = attacker;
        public IEntity Target { get; } = target;
        public float BaseDamage { get; } = baseDamage;
        public AttackResults Result { get; set; }
        public float RawCriticalChance { get; set; }
        public float RawCriticalDamage { get; set; }
        public float AdditionalDamage { get; set; }
        public float FinalDamage { get; set; }
        public bool IsCritical { get; set; }
        public bool ForceCriticalAttack { get; set; }
        public bool IsUnevadable { get; set; }
        public bool IsUnblockable { get; set; }

        public bool IsValid => Target.IsAlive && Attacker.IsAlive;

        public bool Schedule()
        {
            if (!Attacker.IsAlive || !Target.IsAlive) return false;
            AttackContextScheduler.Schedule(this);
            return true;
        }
    }
}
