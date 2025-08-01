namespace Crafting
{
    using Godot;

    [Tool]
    public partial class CraftingPluginEntry : EditorPlugin
    {
        public override void _EnterTree()
        {
            GD.Print("Crafting plugin entered");
        }
    }
}
