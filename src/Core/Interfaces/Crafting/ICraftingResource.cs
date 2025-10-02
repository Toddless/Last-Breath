namespace Core.Interfaces.Crafting
{
    public interface ICraftingResource : IResource
    {
        float Quality { get; set; }
        IMaterialType? MaterialType { get; }
        T Copy<T>();
    }
}
