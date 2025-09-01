namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;
    using Utilities;

    public class CraftingRecipeProvider
    {
        private readonly string _pathToRecipes;
        private Dictionary<EquipmentPart, Dictionary<string, ICraftingRecipe>> _recipes;

        public IReadOnlyDictionary<EquipmentPart, Dictionary<string, ICraftingRecipe>> Recipes => _recipes;

        public CraftingRecipeProvider(string pathToRecipes)
        {
            _pathToRecipes = pathToRecipes;
            _recipes = Enum.GetValues<EquipmentPart>().ToDictionary(key => key, _ => new Dictionary<string, ICraftingRecipe>());
        }


        public ICraftingRecipe? GetRecipe(string id)
        {
            foreach (var dict in Recipes)
            {
                if (dict.Value.TryGetValue(id, out var recipe))
                    return recipe;
            }
            Logger.LogNotFound(id, this);
            return null;
        }

        public void InitializeRecipes()
        {
            using var dir = DirAccess.Open(_pathToRecipes);
            if (dir == null)
            {
                Logger.LogNull(_pathToRecipes, this);
                return;
            }

            dir.ListDirBegin();
            try
            {
                string file;

                while ((file = dir.GetNext()) != "")
                {
                    if (!file.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var path = $"{_pathToRecipes}/{file}";
                    var recipe = ResourceLoader.Load<ICraftingRecipe>(path);
                    if (!TryAddrecipe(recipe))
                    {
                        Logger.LogError($"Failed to add recipe: {recipe.Id}", this);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("Data loading failed.", ex, this);
            }
            finally
            {
                dir.ListDirEnd();
            }
        }

        private bool TryAddrecipe(ICraftingRecipe recipe)
        {
            foreach (var part in Enum.GetValues<EquipmentPart>())
            {
                if (recipe.Tags.Contains(part.ToString()))
                {
                    if (!_recipes[part].TryAdd(recipe.Id, recipe)) return false;
                    return true;
                }
            }
            return false;
        }
    }
}
