namespace Battle
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Components;

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
        public bool IsAttackSucceed { get; set; }

        public void Schedule() => AttackContextScheduler.Schedule(this);
    }
}
