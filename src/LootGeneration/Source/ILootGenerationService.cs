namespace LootGeneration.Source
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Entity;

    public interface ILootGenerationService
    {
        void ChangeLootConfiguration(ILootConfiguration configuration);
        Task<List<ItemStack>> GenerateItemsAsync(IFightable diedEntity);
    }
}
