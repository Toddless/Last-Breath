namespace Core.Interfaces.Entity
{
    using Enums;
    using System;
    using Battle;
    using System.Threading.Tasks;

    public interface IFightable
    {
        ICombatEventDispatcher CombatEvents { get; }
        IStance CurrentStance { get; }

        bool IsFighting { get; set; }
        bool IsAlive { get; }

        event Action<IFightable> Dead;

        Task Attack(IAttackContext context);
        void OnTurnEnd();
        void OnTurnStart();
        Task ReceiveAttack(IAttackContext context);
        void TakeDamage(float damage, DamageType type, DamageSource source, bool isCrit = false);
        void AllAttacks();
        void OnEvadeAttack();
        void OnBlockAttack();
        void Kill();
    }
}
