namespace LootGeneration.Source
{
    using System.Collections.Generic;

    public interface IItemEffectProvider
    {
        List<string> GetCopyItemsEffects();
    }
}
