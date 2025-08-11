namespace Crafting.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Godot;

    public class RecipeManager
    {
        private readonly string _pathToRecipes;
        private Dictionary<EquipmentPart, Dictionary<string, ICraftingRecipe>> _recipes;

        public RecipeManager(string pathToRecipes)
        {
            _pathToRecipes = pathToRecipes;
            _recipes = Enum.GetValues<EquipmentPart>().ToDictionary(key => key, _ => new Dictionary<string, ICraftingRecipe>());
        }


        public void InitializeRecipes()
        {
            //bla bla bla, find all recipes in folder within given path
            var path = "Path";

            var recipe = ResourceLoader.Load<ICraftingRecipe>(path);
        }
    }
}
