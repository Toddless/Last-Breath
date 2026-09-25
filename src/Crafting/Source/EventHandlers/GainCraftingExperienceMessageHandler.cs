namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.MessageBus;
    using Core.MessageBus.Messages;

    /// <summary>The reward numbers (per-rarity exp × mode factor) live in the CraftingMastery
    /// catalog — the mastery computes them, the handler only delivers.</summary>
    public class GainCraftingExperienceMessageHandler(ICraftingMastery craftingMastery)
        : IMessageHandler<GainCraftingExpirienceMessage>
    {
        public Task HandleMessageAsync(GainCraftingExpirienceMessage message)
        {
            craftingMastery.AddExperience(craftingMastery.GetExperienceReward(message.Action, message.ItemRarity));
            return Task.CompletedTask;
        }
    }
}
