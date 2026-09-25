namespace Core.Interfaces
{
    using Enums;

    public interface IRequirement
    {
        RequirementType Type { get; }
        string Id { get; }
        int Amount { get; }
    }
}
