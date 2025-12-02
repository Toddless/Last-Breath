namespace Core.Interfaces.Battle
{
    using System;
    using Components;
    using Entity;

    public interface IAttackContext
    {
        IRandomNumberGenerator Rnd { get; }
        IEntity Attacker { get; }
        IEntity Target { get; }
        float BaseDamage { get; }
        float FinalDamage { get; set; }
        bool IsCritical { get; set; }
        bool IsAttackSucceed { get; set; }

        event Action<IAttackContext>? OnAttackCanceled;
        event Action<IAttackResult>? OnAttackResult;

        void CancelAttack();
        void SetAttackResult(IAttackResult result);
    }
}
