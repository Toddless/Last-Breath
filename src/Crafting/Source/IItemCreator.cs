namespace Crafting.Source
{
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public interface IItemCreator
    {
        IEquipItem CreateEquipItem(string resultItemId, IEnumerable<IMaterialModifier> resourceModifiers, ICharacter? player = null);
        IEquipItem CreateGenericItem(string resultItemId, IEnumerable<IMaterialModifier> resouces, ICharacter? player = null);
        IItem CreateItem(string resultItemId);
    }
}
