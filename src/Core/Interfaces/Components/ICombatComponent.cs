namespace Core.Interfaces.Components
{
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Events;

    public interface ICombatComponent
    {
        IBattleEventBus? BattleEventBus { get; set; }
        IGameEventBus? GameEventBus { get; set; }
        Task ReceiveAttack(IAttackContext context);
        Task Attack(IAttackContext context);
        Task TakeDamage(IDamageContext context);
        void Heal(IHealContext context);
        void OnTurnStart();
        void OnTurnEnd();
    }
}
