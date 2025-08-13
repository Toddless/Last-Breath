namespace LastBreath.addons.Crafting.Resources.Materials
{
    using Godot;
    using Godot.Collections;
    using LastBreath.Addons.Crafting;

    [Tool]
    [GlobalClass]
    public partial class Materials : Resource
    {
        [Export] public Array<MaterialType> AllMaterials = [];
    }
}
