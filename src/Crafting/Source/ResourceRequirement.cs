namespace Crafting.Source
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Crafting;

    [GlobalClass]
    public partial class ResourceRequirement : Resource, IResourceRequirement
    {
        [Export] public RequirementType Type { get; private set; } = RequirementType.ResourceAmount;
        [Export] public string ResourceId { get; private set; } = string.Empty;
        [Export] public int Amount { get; private set; } = 1;

        public ResourceRequirement()
        {

        }

        public ResourceRequirement(RequirementType type, string entityId, int amount = 1)
        {
            Type = type;
            ResourceId = entityId;
            Amount = amount;
        }
    }
}
