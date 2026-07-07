namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Events;

    public record EquipRarityChosenEvent(Dictionary<Rarity, int> RarityAmount) : IGameEvent;
}
