namespace LastBreath.Addons.Crafting
{
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class MaterialModifiers : Resource, IMaterialModifiers
    {
        [Export] public Parameter Parameter { get; set; }
        [Export] public ModifierType ModifierType { get; set; }
        [Export] public float Value { get; set; }
    }
}
