namespace Core.Data.LootTable
{
    using System.Collections.Generic;

    public record TablesData(
        List<LootTableData> General,
        List<LootTableData> Fractions,
        List<LootTableData> Individual,
        List<LootTableData> Types);
}
