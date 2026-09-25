namespace LootGeneration.Internal
{
    using Core.Items;

    public record ItemStack(IItem Item)
    {
        public int Stack { get; set; }
    }
}
