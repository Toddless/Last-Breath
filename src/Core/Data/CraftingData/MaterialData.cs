namespace Core.Data.CraftingData
{
    using System.Collections.Generic;

    public record MaterialData(string Id, string CategoryId, List<MaterialModifierData> Modifiers);
}
