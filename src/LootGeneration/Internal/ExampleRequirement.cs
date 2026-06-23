namespace LootGeneration.Internal
{
    using Core.Enums;
    using Core.Interfaces;
    using Godot;

    internal partial class ExampleRequirement : Resource, IRequirement
    {
        [Export] public RequirementType Type { get; private set; } = RequirementType.Resource;
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public int Amount { get; private set; } = 1;

        public ExampleRequirement()
        {

        }

        public ExampleRequirement(RequirementType type, string entityId, int amount = 1)
        {
            Type = type;
            Id = entityId;
            Amount = amount;
        }
    }
}
