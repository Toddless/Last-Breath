namespace Core.Services
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Items;

    public interface IItemCreationService
    {
        IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier);
        IItem CreateItemByRecipe(string recipeId,  IEnumerable<IModifier> modifiers);
    }
}
