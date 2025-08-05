namespace Crafting
{
    using Crafting.Source;
    using Godot;

    public partial class CraftingUI : Panel
    {
        public override void _Ready()
        {
            var x = new CraftingResourceFactory();
        }
    }
}
