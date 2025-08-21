namespace Crafting.Source.UIElements
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;

    public partial class CraftingUI : Panel
    {
        private List<OptionalResource> _optionalResources = [];
        [Export] private Tree? _recipeTree;
        [Export] private ItemList? _possibleModifiersList;
        [Export] private VBoxContainer? _main, _optional;
        [Export] private Button? _create;

        [Signal] public delegate void RecipeSelectedEventHandler(string id);
        [Signal] public delegate void ItemCreatedEventHandler();

        public override void _Ready()
        {
            _recipeTree?.Clear();
            if (_recipeTree != null) _recipeTree.ItemSelected += OnItemSelected;
            if (_create != null)
            {
                _create.Disabled = true;
                _create.Visible = false;
                _create.Pressed += () => EmitSignal(SignalName.ItemCreated);
            }
            
        }

        public void AddOptionalResource(OptionalResource optional)
        {
            _optional?.AddChild(optional);
            _optionalResources.Add(optional);
        }

        public void ConsumeOptionalResource() => _optionalResources.ForEach(x => x.ConsumeResource());

        public void ClearOptionalResources()
        {
            foreach (var opt in _optionalResources)
            {
                if (opt.CanClear())
                {
                    GD.Print("Resource ready to clear");
                    opt.RemoveCraftingResource();
                }
            }
        }

        public void ClearPossibleModifiers() => _possibleModifiersList?.Clear();
        public void ShowRecipe(IReadOnlyDictionary<ICraftingResource, (int have, int need)> resources)
        {
            ClearResources(_main);
            foreach (var resource in resources)
            {
                var template = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
                template.SetText(resource.Key.DisplayName, resource.Value.have, resource.Value.need);
                if (resource.Key.Icon != null) template.SetIcon(resource.Key.Icon);
                _main?.AddChild(template);
            }
        }

        public void CreatingRecipeTree(IReadOnlyDictionary<EquipmentPart, Dictionary<string, ICraftingRecipe>>? recipes)
        {
            if (_recipeTree == null || recipes == null)
            {
                // TODO: Log
                return;
            }
            var treeRoot = _recipeTree.CreateItem();

            foreach (var part in recipes)
            {
                var category = _recipeTree.CreateItem(treeRoot);
                // for now enum as category name, think about localization later
                category.SetText(0, part.Key.ToString());
                category.SetSelectable(0, false);
                foreach (var res in part.Value)
                {
                    var recipe = _recipeTree.CreateItem(category);
                    recipe.SetText(0, res.Value.Name);
                    recipe.SetMetadata(0, res.Value.Id);
                    recipe.SetSelectable(0, true);
                }
            }
        }

        public void DestroingRecipeTree() => _recipeTree?.Clear();

        public void SetCreateButtonState(bool canUse)
        {
            if (_create != null)
            {
                _create.Disabled = !canUse;
                _create.Visible = canUse;
            }
        }

        private void ClearResources(VBoxContainer? container)
        {
            foreach (var child in container?.GetChildren() ?? [])
                child.QueueFree();
        }

        private void OnItemSelected()
        {
            var selected = _recipeTree?.GetSelected();
            if (selected == null) return;
            EmitSignal(SignalName.RecipeSelected, selected.GetMetadata(0));
        }

        public void ShowModifiers(HashSet<IMaterialModifier> modifiers)
        {
            _possibleModifiersList?.Clear();
            foreach (var modifier in modifiers)
            {
                // TODO: Localization, formatting
                _possibleModifiersList?.AddItem($"{modifier.Parameter} : {modifier.Value}");
            }
        }
    }
}
