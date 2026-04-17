namespace Crafting.Source
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;

    [GlobalClass]
    public partial class Requirement : Resource, IRequirement
    {
        [Export] public RequirementType Type { get; private set; } = RequirementType.Resource;
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public int Amount { get; private set; } = 1;

        public Requirement()
        {

        }

        public Requirement(RequirementType type, string requirementId, int amount = 1)
        {
            Type = type;
            Id = requirementId;
            Amount = amount;
        }
    }
}
