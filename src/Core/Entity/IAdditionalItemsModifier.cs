namespace Core.Entity
{
    using System.Collections.Generic;
    using Data.LootTable;

    public interface IAdditionalItemsModifier : INpcModifier
    {
        Dictionary<int, List<TableRecord>> Items { get; }
    }
}
