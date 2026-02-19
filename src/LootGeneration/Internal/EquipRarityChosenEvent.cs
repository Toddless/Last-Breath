namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using Core.Interfaces.Events;
    using Core.Enums;

    public record EquipRarityChosenEvent(Dictionary<Rarity, int> RarityAmount) : IGameEvent;
}
