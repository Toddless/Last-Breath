namespace LootGeneration.Internal
{
    using Core.Interfaces.Items;

    public record ItemStack(IItem Item)
    {
        public int Stack { get; set; }
    }
}
