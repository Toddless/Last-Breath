namespace Core.Crafting
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Which crafting recipes the player can actually use. Knowledge is the union of two sources:
    /// recipes the mastery ladder has unlocked (derived, never persisted) and recipes learned from
    /// scroll items (persisted per playthrough). The create handler is the gate; the UI mirrors.
    /// </summary>
    public interface IRecipeKnowledge
    {
        /// <summary>Fires only for scroll learns; mastery unlocks announce themselves through the
        /// mastery level-change events instead.</summary>
        event Action<string>? RecipeLearned;

        /// <summary>Scroll-learned ids only — the persisted part of knowledge.</summary>
        IReadOnlyCollection<string> LearnedIds { get; }

        bool IsKnown(string recipeId);

        /// <summary>False when the recipe is already known (mastery-derived or learned) — the
        /// caller keeps the scroll intact in that case.</summary>
        bool Learn(string recipeId);

        void RestoreState(IEnumerable<string> learnedIds);
    }
}
