namespace Crafting.Source.UIElements
{
    using Godot;

    public partial class CraftingUI : Panel
    {
        [Export] private Tree? _recipeTree;

        public override void _Ready()
        {
            _recipeTree?.Clear();

        }
    }
}
