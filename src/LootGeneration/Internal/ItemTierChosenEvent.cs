namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using Core.Events;

    public record ItemTierChosenEvent(Dictionary<int, int> ChosenTiersAmount): IGameEvent;
}
