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


    }
}
