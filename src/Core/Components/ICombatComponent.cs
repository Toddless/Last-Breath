namespace Core.Components
{
    using Events;

    public interface ICombatComponent
    {
        IBattleEventBus? BattleEventBus { get; set; }
        IGameEventBus? GameEventBus { get; set; }


    }
}
