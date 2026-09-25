namespace Core.Events
{
    using System.Collections.Generic;
    using Entity;

    public record BattleInitializedEvent(IFightable Player, List<IFightable> Entities) : IGameEvent;
}
