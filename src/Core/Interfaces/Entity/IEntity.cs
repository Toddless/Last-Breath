namespace Core.Interfaces.Entity
{
    using System;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;

    public interface IEntity : IIdentifiable, IDisplayable, IFightable
    {
        IEffectsManager Effects { get; }
        IModifierManager Modifiers { get; }

        bool CanMove { get; set; }

        event Action<IEntity>? Dead;
        event Action<IAttackContext>? BeforeAttack, AfterAttack;
        event Action<IOnGettingAttackEventArgs>? GettingAttack;

        void OnTurnEnd();
        void OnTurnStart();
        void AddItemToInventory(IItem item);
        void OnReceiveAttack(IAttackContext context);
        void TakeDamage(float damage, bool isCrit = false);
        void AllAttacks();
        void OnEvadeAttack();
        void OnBlockAttack();
    }
}
