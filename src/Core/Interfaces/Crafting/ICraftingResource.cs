namespace Core.Interfaces.Crafting
{
    public interface ICraftingResource : IResource
    {
        IMaterial? Material { get; }

        T Copy<T>();
    }
}
