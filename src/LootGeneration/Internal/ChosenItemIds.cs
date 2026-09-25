namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using Core.Events;

    internal record ChosenItemIds(List<string> Items) : IGameEvent;
}
