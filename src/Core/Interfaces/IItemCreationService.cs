namespace Core.Interfaces
{
    using Enums;
    using Items;
    using Modifiers;
    using System.Collections.Generic;

    public interface IItemCreationService
    {
        IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier);
        IItem CreateItemByRecipe(string recipeId,  IEnumerable<IModifier> modifiers);
    }
}
