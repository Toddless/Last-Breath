namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Enums;
    using Core.Events;
    using Godot;

    public class GainCraftingExperienceMessageHandler(ICraftingMastery craftingMastery)
        : IMessageHandler<GainCraftingExpirienceMessage>
    {
        public Task HandleMessageAsync(GainCraftingExpirienceMessage message)
        {
            int bonusExp = Mathf.RoundToInt(GetRarityBonus(message.ItemRarity) * GetCraftingModeFactor(message.Action));
            craftingMastery.AddExperience(bonusExp);
            return Task.CompletedTask;
        }

        private int GetRarityBonus(Rarity rarity) => rarity switch
        {
            Rarity.Uncommon => 10,
            Rarity.Rare => 10 * 2,
            Rarity.Epic => 10 * 3,
            Rarity.Legendary => 10 * 4,
            _ => 10
        };

        private float GetCraftingModeFactor(CraftingMode craftingMode) => craftingMode switch
        {
            CraftingMode.Create => 1,
            CraftingMode.Upgrade => 0.3f,
            CraftingMode.Recraft => 0.3f,
            CraftingMode.Shatter => 0.5f,
            _ => 1
        };
    }
}
