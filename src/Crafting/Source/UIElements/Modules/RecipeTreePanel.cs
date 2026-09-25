namespace Crafting.Source.UIElements.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Localization;
    using Godot;

    /// <summary>The left pane of the crafting window: recipe tree grouped by category, with search
    /// and the shown-recipes counter. Unknown recipes stay visible but locked. A picked recipe
    /// travels up as an event; the catalogs are only read.</summary>
    [GlobalClass]
    public partial class RecipeTreePanel : PanelContainer
    {
        private const string LockedByMasteryKey = "UI_Recipe_Locked_Mastery";
        private const string LockedByScrollKey = "UI_Recipe_Locked_Scroll";
        private const string RecipesTitleKey = "UI_Craft_Recipes";

        [Export] private Label? _listTitle;
        [Export] private LineEdit? _search;
        [Export] private Tree? _tree;

        private IItemDataProvider? _dataProvider;
        private IInventory? _inventory;
        private IRecipeKnowledge? _knowledge;

        public event Action<string>? RecipeSelected;

        public override void _Ready()
        {
            _tree?.ItemSelected += OnItemSelected;
            _search?.TextChanged += _ => Rebuild();
        }

        /// <summary>The catalogs the tree is built from; the panel rebuilds itself right away.</summary>
        public void SetCatalog(IItemDataProvider? dataProvider, IInventory? inventory, IRecipeKnowledge? knowledge)
        {
            _dataProvider = dataProvider;
            _inventory = inventory;
            _knowledge = knowledge;
            Rebuild();
        }

        /// <summary>Rebuilds the tree from the catalogs, honouring the current search query.</summary>
        public void Rebuild()
        {
            if (_tree == null || _dataProvider == null) return;

            string query = _search?.Text?.Trim() ?? string.Empty;
            _tree.Clear();
            _tree.HideRoot = true;
            var root = _tree.CreateItem();
            int totalShown = 0;

            foreach (var group in _dataProvider.GetCraftingRecipes()
                         .GroupBy(RecipeCategory)
                         .OrderBy(entry => entry.Key.ToString()))
            {
                var matching = group
                    .Where(recipe => query.Length == 0 || Localization.Localize(recipe.Id).Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matching.Count == 0) continue;

                var category = _tree.CreateItem(root);
                category.SetText(0, Localization.Localize(group.Key.ToString()));
                category.SetSelectable(0, false);
                totalShown += matching.Count;

                foreach (var recipe in matching)
                {
                    var entry = _tree.CreateItem(category);
                    int amount = CraftableAmount(recipe.Id);
                    entry.SetText(0, amount > 0 ? $"{Localization.Localize(recipe.Id)} ({amount})" : Localization.Localize(recipe.Id));
                    entry.SetMetadata(0, recipe.Id);

                    // Unknown recipes stay visible but locked — the hint says how to get them.
                    bool known = _knowledge?.IsKnown(recipe.Id) ?? true;
                    entry.SetSelectable(0, known);
                    if (known) continue;
                    entry.SetCustomColor(0, Color.FromHtml(TextPalette.System));
                    entry.SetTooltipText(0, recipe.UnlockAtMastery is { } gate
                        ? Localization.Render(LockedByMasteryKey, new Dictionary<string, object?> { ["Level"] = gate })
                        : Localization.Localize(LockedByScrollKey));
                }
            }

            _listTitle?.Text = $"{Localization.Localize(RecipesTitleKey)} ({totalShown})";
        }

        public void Deselect() => _tree?.DeselectAll();

        private void OnItemSelected()
        {
            string recipeId = _tree?.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            if (recipeId.Length == 0) return;
            RecipeSelected?.Invoke(recipeId);
        }

        private static RecipeCategories RecipeCategory(ICraftingRecipe recipe) =>
            Enum.GetValues<RecipeCategories>()
                .FirstOrDefault(category => recipe.Tags.Contains(category.ToString(), StringComparer.OrdinalIgnoreCase));

        private int CraftableAmount(string recipeId)
        {
            var requirements = _dataProvider?.GetRecipeRequirements(recipeId) ?? [];
            int amount = int.MaxValue;
            foreach (var requirement in requirements)
            {
                int owned = requirement.Type switch
                {
                    RequirementType.Resource => _inventory?.GetTotalItemAmount(requirement.Id) ?? 0,
                    // The optimistic ceiling: any owned member of the category counts toward the slot.
                    RequirementType.ResourceCategory => CategoryOwnedTotal(requirement.Id),
                    _ => -1,
                };
                if (owned < 0) continue;
                amount = Math.Min(amount, owned / requirement.Amount);
            }

            return amount == int.MaxValue ? 0 : amount;
        }

        private int CategoryOwnedTotal(string categoryId) =>
            CategoryResources.CandidateIds(_dataProvider, _inventory, categoryId)
                .Distinct()
                .Sum(id => _inventory?.GetTotalItemAmount(id) ?? 0);
    }
}
