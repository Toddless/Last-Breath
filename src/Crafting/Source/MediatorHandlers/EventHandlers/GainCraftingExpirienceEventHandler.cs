namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Mediator.Events;

    public class GainCraftingExpirienceEventHandler : IEventHandler<GainCraftingExpirienceEvent>
    {
        private readonly ICraftingMastery _craftingMastery;

        public GainCraftingExpirienceEventHandler(ICraftingMastery craftingMastery)
        {
            _craftingMastery = craftingMastery;
        }

        public void Handle(GainCraftingExpirienceEvent evnt)
        {
            var bonusExp = Mathf.RoundToInt(GetRarityBonus(evnt.ItemRarity) * GetCraftingModeFactor(evnt.Action));
            _craftingMastery.AddExpirience(bonusExp);
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
