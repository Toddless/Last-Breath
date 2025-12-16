namespace Core.Interfaces.Entity
{
    using Enums;
    using System;
    using System.Collections.Generic;
    using Battle;
    using System.Threading.Tasks;
    using Events;

    public interface IFightable
    {
        ICombatEventBus CombatEvents { get; }
        IStance CurrentStance { get; }
        ITargetChooser? TargetChooser { get; set; }

        bool IsFighting { get; set; }
        bool IsAlive { get; }

        event Action<IFightable> Dead;
        event Action<float, DamageType, bool> DamageTaken;

        Task Attack(IAttackContext context);
        void SetupEventBus(IBattleEventBus bus);
        void OnTurnEnd();
        void OnTurnStart();
        Task ReceiveAttack(IAttackContext context);
        void TakeDamage(IEntity from, float damage, DamageType type, DamageSource source, bool isCrit = false);
        IEntity ChoseTarget(List<IEntity> targets);
        void Kill();
    }
}
