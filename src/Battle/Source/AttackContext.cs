namespace Battle.Source
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;

    public class AttackContext(IEntity attacker, IEntity target, float baseDamage, IRandomNumberGenerator rnd, IAttackContextScheduler attackContextScheduler)
        : IAttackContext
    {
        public IRandomNumberGenerator Rnd { get; } = rnd;
        public IEntity Attacker { get; } = attacker;
        public IEntity Target { get; } = target;
        public float BaseDamage { get; } = baseDamage;
        public float AdditionalDamage { get; set; } = 0f;
        public float FinalDamage { get; set; } = baseDamage;
        public IAttackContextScheduler AttackContextScheduler { get; } = attackContextScheduler;
        public bool IsCritical { get; set; }
        public AttackResults Result { get; set; }

        public bool IsValid => Target.IsAlive && Attacker.IsAlive;

        public bool Schedule()
        {
            if (!Attacker.IsAlive || !Target.IsAlive) return false;

            AttackContextScheduler.Schedule(this);
            return true;
        }
    }
}
