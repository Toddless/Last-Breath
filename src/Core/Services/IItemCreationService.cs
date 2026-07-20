namespace Core.Services
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Items;

    /// <summary>
    ///
    /// </summary>
    public interface IItemCreationService
    {
        IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier);

        /// <summary><paramref name="minRarity"/> — the rarity floor from creation runes: the mastery roll
        /// is raised to it when worse, never lowered (lower enum value = better).</summary>
        IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors, Rarity? minRarity = null);
    }
}
