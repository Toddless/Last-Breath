namespace Core.Interfaces
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Items;

    public interface IItemCreationService
    {
        IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance);
        IItem CreateItem(string id);
    }
}
