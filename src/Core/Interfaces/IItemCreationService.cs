namespace Core.Interfaces
{
    using System.Collections.Generic;
    using Enums;
    using Items;
    using Modifiers;

    public interface IItemCreationService
    {
        IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier);
        IItem CreateItemByRecipe(string recipeId,  IEnumerable<IModifier> modifiers);
    }
}
