namespace LastBreath.Addons.Crafting.Resources.Materials
{
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class MaterialModifier : Resource, IMaterialModifier
    {
        [Export] public Parameter Parameter { get; set; }
        [Export] public ModifierType ModifierType { get; set; }
        [Export] public float Value { get; set; }
    }
}
