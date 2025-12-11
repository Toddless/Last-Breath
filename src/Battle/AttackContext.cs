namespace Battle
{
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Components;

    public class AttackContext(IEntity attacker, IEntity target, float baseDamage, IRandomNumberGenerator rnd, ICombatScheduler combatScheduler)
        : IAttackContext
    {
        public IRandomNumberGenerator Rnd { get; } = rnd;
        public IEntity Attacker { get; } = attacker;
        public IEntity Target { get; } = target;
        public float BaseDamage { get; } = baseDamage;
        public float FinalDamage { get; set; } = baseDamage;
        public ICombatScheduler CombatScheduler { get; } = combatScheduler;
        public bool IsCritical { get; set; }
        public bool IsAttackSucceed { get; set; }

        public void Schedule() => CombatScheduler.Schedule(this);
    }
}
