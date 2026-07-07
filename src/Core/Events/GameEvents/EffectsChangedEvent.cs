namespace Core.Events.GameEvents
{
    using System.Collections.Generic;
    using Views;
    using Entity;

    /// <summary>The aggregated effect list of a fighter changed (add/remove/duration). UI rebuilds its icons from it.</summary>
    public record EffectsChangedEvent(IFightable Target, IReadOnlyList<EffectView> Effects) : IBattleEvent, IGameEvent;
}
