namespace Core.Interfaces.Crafting
{
    public interface IResourceRequirement : IRequirement
    {
        string ResourceId {  get; }
        int Amount { get; }
    }
}
