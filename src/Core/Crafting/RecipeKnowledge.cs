namespace Core.Crafting
{
    using System;
    using System.Collections.Generic;
    using Data;
    using Session;

    /// <summary>
    /// Mastery-derived knowledge is computed on every query (no cached ladder — bonus levels and
    /// data reloads keep it honest); only scroll learns are state. A recipe with no
    /// <c>unlockAtMastery</c> in data can ONLY arrive through a scroll.
    /// </summary>
    public class RecipeKnowledge(IItemDataProvider recipes, ICraftingMastery mastery) : IRecipeKnowledge, ISessionResettable
    {
        private readonly HashSet<string> _learned = [];

        public event Action<string>? RecipeLearned;

        public IReadOnlyCollection<string> LearnedIds => _learned;

        public bool IsKnown(string recipeId) =>
            _learned.Contains(recipeId) || MasteryUnlocked(recipeId);

        public bool Learn(string recipeId)
        {
            if (IsKnown(recipeId)) return false;

            _learned.Add(recipeId);
            RecipeLearned?.Invoke(recipeId);
            return true;
        }

        public void RestoreState(IEnumerable<string> learnedIds)
        {
            _learned.Clear();
            foreach (string id in learnedIds) _learned.Add(id);
        }

        public void ResetSession() => _learned.Clear();

        /// <summary>Earned + bonus levels count toward the ladder — same reading as the ascension gate.</summary>
        private bool MasteryUnlocked(string recipeId) =>
            FindRecipe(recipeId)?.UnlockAtMastery is { } gate && mastery.CurrentLevel + mastery.BonusLevel >= gate;

        private ICraftingRecipe? FindRecipe(string recipeId)
        {
            foreach (var recipe in recipes.GetCraftingRecipes())
                if (recipe.Id == recipeId)
                    return recipe;
            return null;
        }
    }
}
