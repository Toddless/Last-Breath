namespace LastBreath.addons.Crafting.Recipies
{
    using Godot;
    using Godot.Collections;
    using Core.Interfaces.Crafting;

    [Tool]
    [GlobalClass]
    public partial class CraftingRecipe : Resource, ICraftingRecipe
    {
        [Export] private Array<RecipeRequirement> _mainResources = [];
        [Export] public string Id { get; private set; } = string.Empty;
        [Export] public string ResultItemId { get; private set; } = string.Empty;
        [Export] public string[] Tags { get; private set; } = [];
        public string DisplayName => GetLocalizedName();
        public string Description => GetLocalizedDescription();

        public System.Collections.Generic.List<IRecipeRequirement> MainResource => [.. _mainResources];
        private string GetLocalizedName() => TranslationServer.Translate(Id);
        private string GetLocalizedDescription() => TranslationServer.Translate(Id + $"_{nameof(Description)}");
    }
}
