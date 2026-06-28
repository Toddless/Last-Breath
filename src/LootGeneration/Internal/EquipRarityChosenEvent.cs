namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Events;

    public record EquipRarityChosenEvent(Dictionary<Rarity, int> RarityAmount) : IGameEvent;
}
