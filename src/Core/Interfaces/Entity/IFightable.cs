namespace Core.Interfaces.Entity
{
    using Enums;
    using System;
    using System.Collections.Generic;
    using Battle;
    using System.Threading.Tasks;

    public interface IFightable
    {
        IEventBus Events { get; }
        IStance CurrentStance { get; }
        ITargetChooser? TargetChooser { get; set; }

        bool IsFighting { get; set; }
        bool IsAlive { get; }

        event Action<IFightable> Dead;

        Task Attack(IAttackContext context);
        void OnTurnEnd();
        void OnTurnStart();
        Task ReceiveAttack(IAttackContext context);
        void TakeDamage(float damage, DamageType type, DamageSource source, bool isCrit = false);
        IEntity ChoseTarget(List<IEntity> targets);
        void Kill();
    }
}
