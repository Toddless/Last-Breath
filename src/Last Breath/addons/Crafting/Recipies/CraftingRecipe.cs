namespace LastBreath.addons.Crafting.Recipies
{
    using Godot;
    using Godot.Collections;
    using Core.Interfaces.Crafting;

    [GlobalClass]
    public partial class CraftingRecipe : Resource, ICraftingRecipe
    {
        [Export] private Array<RecipeRequirement> _mainResources = [];
        [Export] private Array<RecipeRequirement> _optionalResources = [];
        [Export] public string Id = string.Empty;
        [Export] public string ResultItemId = string.Empty;

        public string Name => GetLocalizedName();
        public string Description => GetLocalizedDescription();

        public System.Collections.Generic.List<IRecipeRequirement> MainResource => [.. _mainResources];
        public System.Collections.Generic.List<IRecipeRequirement> OptionalResources => [.. _optionalResources];

        private string GetLocalizedName() => TranslationServer.Translate(Id);
        private string GetLocalizedDescription() => TranslationServer.Translate(Id + $"_{nameof(Description)}");
    }
}
