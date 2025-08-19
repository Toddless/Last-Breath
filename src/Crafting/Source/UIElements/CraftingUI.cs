namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;

    public partial class CraftingUI : Panel
    {
        private readonly HashSet<IMaterialModifier> _mainModifiers = [];
        private readonly HashSet<IMaterialModifier> _optionalModifiers = [];
        [Export] private Tree? _recipeTree;
        [Export] private ItemList? _possibleModifiersList;
        [Export] private VBoxContainer? _main, _optional;
        private Action? _removeModifiersFromCacheCallBack;

        [Signal] public delegate void RecipeSelectedEventHandler(string id);
        [Signal] public delegate void ItemCreatedEventHandler();

        public override void _Ready()
        {
            _recipeTree?.Clear();
            if (_recipeTree != null)
            {
                _recipeTree.ItemSelected += OnItemSelected;
            }
        }

        public void AddOptionalResource(OptionalResource optional) => _optional?.AddChild(optional);

        public void OnResourceAdded(ICraftingResource resource)
        {
            AddModifiersToSet(_optionalModifiers, resource.MaterialType?.Modifiers ?? []);
            UpdateModifiers();
        }

        public void OnResourceRemoved(ICraftingResource resource)
        {
            RemoveModifiersFromSet(_optionalModifiers, resource.MaterialType?.Modifiers ?? []);
            UpdateModifiers();
        }

        public void ShowRecipe(ICraftingRecipe recipe, IReadOnlyDictionary<ICraftingResource, int> mainRes)
        {
            _removeModifiersFromCacheCallBack?.Invoke();
            _removeModifiersFromCacheCallBack = null;
            _possibleModifiersList?.Clear();
            ClearResources(_main);
            foreach (var item in mainRes)
            {
                var template = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
                template.SetText(item.Key.DisplayName, recipe.MainResource.Find(x => x.CraftingResourceId == item.Key.Id)?.Amount ?? 0, item.Value);
                if (item.Key.Icon != null)
                    template.SetIcon(item.Key.Icon);
                _main?.AddChild(template);
                AddModifiersToSet(_mainModifiers,item.Key.MaterialType?.Modifiers ?? []);
            }

            _removeModifiersFromCacheCallBack = () =>
            {
                foreach (var res in mainRes.Keys)
                    RemoveModifiersFromSet(_mainModifiers, res.MaterialType?.Modifiers ?? []);
            };
            UpdateModifiers();
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

        private void ClearResources(VBoxContainer? container)
        {
            foreach (var chil in container?.GetChildren() ?? [])
                chil.QueueFree();
        }

        private void OnItemSelected()
        {
            var selected = _recipeTree?.GetSelected();
            if (selected == null) return;
            EmitSignal(SignalName.RecipeSelected, selected.GetMetadata(0));
        }

        private void AddModifiersToSet(HashSet<IMaterialModifier> set ,IReadOnlyList<IMaterialModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                if (set.Contains(modifier)) continue;
                set.Add(modifier);
            }
        }

        private void RemoveModifiersFromSet(HashSet<IMaterialModifier> set, IReadOnlyList<IMaterialModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                set.Remove(modifier);
            }
        }

        private void UpdateModifiers()
        {
            if (_possibleModifiersList == null) return;
            _possibleModifiersList.Clear();
            var modifiers = _mainModifiers.Concat(_optionalModifiers).ToHashSet();
            foreach (var modifier in modifiers)
            {
                _possibleModifiersList!.AddItem($"{modifier.Parameter} : {modifier.Value}");
            }
        }
    }
}
