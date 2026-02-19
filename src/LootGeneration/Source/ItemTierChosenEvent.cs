namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Interfaces.Events;

    public record ItemTierChosenEvent(Dictionary<int, int> ChosenTiersAmount): IGameEvent;
}
