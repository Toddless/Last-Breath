namespace Core.Interfaces.Crafting
{
    public interface IResourceRequirement
    {
        int Amount { get; }
        string ResourceId { get; }
    }
}
