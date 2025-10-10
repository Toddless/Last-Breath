namespace Crafting.Source
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;

    public interface IItemCreator
    {
        IEquipItem? CreateEquipItem(string resultItemId, IEnumerable<IMaterialModifier> resources, ICharacter? player = null);
        IEquipItem? CreateGenericItem(string resultItemId, IEnumerable<IMaterialModifier> resouces, ICharacter? player = null);
        IItem? CreateItem(string resultItemId);
    }
}