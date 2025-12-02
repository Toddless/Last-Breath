namespace Battle
{
    using System;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Components;

    public class AttackContext(IEntity attacker, IEntity target, float baseDamage, IRandomNumberGenerator rnd) : IAttackContext
    {
        public IRandomNumberGenerator Rnd { get; } = rnd;
        public IEntity Attacker { get; } = attacker;
        public IEntity Target { get; } = target;
        public float BaseDamage { get; } = baseDamage;
        public float FinalDamage { get; set; }
        public bool IsCritical { get; set; }
        public bool IsAttackSucceed { get; set; }

        public event Action<IAttackResult>? OnAttackResult;
        public event Action<IAttackContext>? OnAttackCanceled;

        public void SetAttackResult(IAttackResult result) => OnAttackResult?.Invoke(result);
        public void CancelAttack() => OnAttackCanceled?.Invoke(this);
    }
}
