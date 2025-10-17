namespace Core.Interfaces
{
    using Core.Enums;

    public interface IRequirement
    {
        RequirementType Type { get; }
    }
}
