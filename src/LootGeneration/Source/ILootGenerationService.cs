namespace LootGeneration.Source
{
    using Internal;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using System.Collections.Generic;

    public interface ILootGenerationService
    {
        void ChangeLootConfiguration(ILootConfiguration configuration);
        Task<List<ItemStack>> GenerateItemsAsync(IEntity diedEntity);
    }
}
