namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;

    [GlobalClass]
    public partial class Recipes : PanelContainer, IInitializable, IRequireServices
    {
        private const string UID = "uid://1qq3je71i5vi";
        private Dictionary<RecipeCategories, TreeItem> _categories = [];
        [Export] private RecipeTree? _recipeTree;

        private IItemDataProvider? _dataProvider;
        private IGameMessageBus? _mediator;

        [Signal]
        public delegate void RecipeSelectedEventHandler(string id);

        public override void _Ready()
        {
            if (_recipeTree != null)
            {
                _recipeTree.LeftClick += OnLeftClick;
                _recipeTree.RightClick += OnRightClick;
            }

            CreateRecipeTree(_dataProvider?.GetCraftingRecipes() ?? []);
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _mediator = provider.GetService<IGameMessageBus>();
        }

        public override void _EnterTree()
        {
            //if (_mediator != null) _mediator.UpdateUi += UpdateRecipeTree;
        }

        public override void _ExitTree()
        {
           // if (_mediator != null) _mediator.UpdateUi -= UpdateRecipeTree;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async void CreateRecipeTree(IEnumerable<ICraftingRecipe> recipes)
        {
            if (_recipeTree == null)
            {
                Tracker.TrackNull(nameof(_recipeTree), this);
                return;
            }

            _recipeTree.Clear();
            _categories.Clear();

            var treeRoot = _recipeTree.CreateItem();
            foreach (var cat in Enum.GetValues<RecipeCategories>())
            {
                var category = _recipeTree.CreateItem(treeRoot);
                category.SetText(0, Localization.Localize(cat.ToString()));
                category.SetSelectable(0, false);
                category.SetMetadata(0, "category");
                _categories[cat] = category;
            }

            foreach (var recipe in recipes)
            {
                var category = _categories.Keys.FirstOrDefault(category =>
                    recipe.Tags.Contains(category.ToString(), StringComparer.OrdinalIgnoreCase));
                var treeItem = _recipeTree.CreateItem(_categories[category]);
                var amount = await CalculateAmountToCraft(recipe.MainResource);
                var recipeName = $"{Localization.Localize(recipe.Id)}";
                if (amount > 0)
                    recipeName += $" ({amount})";
                treeItem.SetText(0, recipeName);
                treeItem.SetMetadata(0, recipe.Id);
                treeItem.SetSelectable(0, recipe.IsOpened);
            }
        }

        private async void OnRightClick(string id, TreeItem treeItem)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(_mediator);

                var requirements = _dataProvider?.GetRecipeRequirements(id) ?? [];
                var amount = await CalculateAmountToCraft(requirements);
                if (amount > 1)
                {
                    var item = await _mediator.Send<CreateEquipItemRequest, IEquipItem?>(new CreateEquipItemRequest(id,
                        requirements.ToDictionary(key => key.ResourceId, value => value.Amount)));
                    if (item != null)
                    {
                        _mediator?.PublishAsync(new ItemCreatedEvent(item));
                    }
                }
            }
            catch (Exception ex)
            {
                Tracker.TrackException("On right click failed", ex, this);
            }
        }

        private void OnLeftClick(string id) => EmitSignal(SignalName.RecipeSelected, id);

        private async Task<int> CalculateAmountToCraft(IEnumerable<IResourceRequirement> requirements)
        {
            ArgumentNullException.ThrowIfNull(_mediator);
            int canCraft = int.MaxValue;
            var itemsInInventory =
                await _mediator.Send<GetTotalItemAmountRequest, Dictionary<string, int>>(
                    new(requirements.Select(x => x.ResourceId)));
            foreach (var requirement in requirements)
            {
                if (itemsInInventory.TryGetValue(requirement.ResourceId, out var have))
                {
                    int amountToCraft = have / requirement.Amount;
                    canCraft = Mathf.Min(canCraft, amountToCraft);
                }
            }

            return canCraft;
        }

        private void UpdateRecipeTree()
        {
            var root = _recipeTree?.GetRoot();
            UpdateTreeItem(root);

            async void UpdateTreeItem(TreeItem? item)
            {
                if (item == null) return;

                string meta = item.GetMetadata(0).AsString();

                if (!meta.Equals("category", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(meta))
                {
                    var requirements = _dataProvider?.GetRecipeRequirements(meta) ?? [];
                    var amount = await CalculateAmountToCraft(requirements);
                    var recipeName = $"{Localization.Localize(meta)}";
                    if (amount > 0)
                        recipeName += $" ({amount})";
                    item.SetText(0, recipeName);
                }

                var child = item.GetChildren();
                foreach (var c in child)
                    UpdateTreeItem(c);
            }
        }
    }
}
