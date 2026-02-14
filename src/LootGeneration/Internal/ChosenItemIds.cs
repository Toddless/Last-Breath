namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using Core.Interfaces.Events;

    internal record ChosenItemIds(List<string> Items) : IGameEvent;
}
