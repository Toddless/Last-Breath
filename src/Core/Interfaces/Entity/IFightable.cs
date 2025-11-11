namespace Core.Interfaces.Entity
{
    using System;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;

    public interface IFightable
    {
        IHealthComponent Health {  get; }
        IDamageComponent Damage {  get; }
        IDefenceComponent Defence {  get; }
        IStance CurrentStance { get; }

        bool IsFighting { get; set; }
        bool IsAlive {  get; set; }

        event Action? TurnStart, TurnEnd;
        event Action<IAttackContext>? BeforeAttack, AfterAttack;
        event Action<IOnGettingAttackEventArgs>? GettingAttack;
        event Action<IFightable> Dead;

        void OnTurnEnd();
        void OnTurnStart();
        void OnReceiveAttack(IAttackContext context);
        void TakeDamage(float damage, bool isCrit = false);
        void AllAttacks();
        void OnEvadeAttack();
        void OnBlockAttack();
    }
}
