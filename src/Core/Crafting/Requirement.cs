namespace Core.Crafting
{
    using Enums;
    using Interfaces;

    public class Requirement(RequirementType type, string requirementId, int amount = 1) : IRequirement
    {
        public RequirementType Type { get; } = type;
        public string Id { get; } = requirementId;
        public int Amount { get; } = amount;
    }
}
