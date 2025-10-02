namespace LastBreath.addons.Crafting.Recipies
{
    using Godot;
    using Core.Interfaces.Crafting;

    [Tool]
    [GlobalClass]
    public partial class ResourceRequirement : Resource, IResourceRequirement
    {
        [Export] public int Amount { get; set; } = 1;
        [Export] public string ResourceId { get; set; } = string.Empty;
    }
}
