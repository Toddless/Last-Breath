namespace LootGeneration.Source
{
    using Core.Enums;
    using Core.Data.LootTable;
    using System.Collections.Generic;
    using Core.Interfaces.MessageBus;

    public record GetLootTableRequest(Fractions Fraction, EntityType Type, string Id) : IRequest<Dictionary<int, List<TableRecord>>>;

}
