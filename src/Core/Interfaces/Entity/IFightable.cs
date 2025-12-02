namespace Core.Interfaces.Entity
{
    using Enums;
    using System;
    using Battle;
    using System.Threading.Tasks;

    public interface IFightable
    {
        IStance CurrentStance { get; }

        bool IsFighting { get; set; }
        bool IsAlive { get; set; }

        event Action? TurnStart, TurnEnd;
        event Action<IAttackContext>? BeforeAttack, AfterAttack;
        event Action<IFightable> Dead;

        Task Attack(IEntity target);
        void OnTurnEnd();
        void OnTurnStart();
        void OnReceiveAttack(IAttackContext context);
        void TakeDamage(float damage, DamageType type, DamageSource source, bool isCrit = false);
        void AllAttacks();
        void OnEvadeAttack();
        void OnBlockAttack();
        void Kill();
    }
}
