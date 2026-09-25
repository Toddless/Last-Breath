namespace Core.Crafting
{
    public interface ICraftingResource : IResource
    {
        IMaterial? Material { get; }

        T Copy<T>();
    }
}
