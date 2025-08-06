namespace Crafting.Source
{
    using Godot;

    [Tool]
    public partial class CraftingPluginEntry : EditorPlugin
    {
        public override void _EnterTree()
        {
            GD.Print("Crafting plugin working");
        }

        public override void _ExitTree()
        {
            GD.Print("Crafting leaving tree");
        }
    }
}
