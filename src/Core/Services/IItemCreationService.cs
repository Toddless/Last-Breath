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
        /// <param name="rarity">The rarity ROLLED for this drop — what a piece of equipment is stamped
        /// with and what buys its affix lines.</param>
        /// <param name="fixedRarity">The rarity the bought POSITION already decided, for the kinds that
        /// would otherwise draw their own (an augment rolls a band). Null when the position named one
        /// thing by id and so said nothing about what it is worth.</param>
        IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier, Rarity? fixedRarity = null);

        /// <summary><paramref name="minRarity"/> — the rarity floor from creation runes: the mastery roll
        /// is raised to it when worse, never lowered (lower enum value = better).</summary>
        IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors, Rarity? minRarity = null);
    }
}
