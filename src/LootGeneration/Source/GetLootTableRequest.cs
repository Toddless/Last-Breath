namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using Core.Data.LootTable;
    using Core.Enums;
    using Core.Interfaces.MessageBus;

    public record GetLootTableRequest(Fractions Fraction, EntityType Type, string Id) : IRequest<Dictionary<int, List<TableRecord>>>;

}
