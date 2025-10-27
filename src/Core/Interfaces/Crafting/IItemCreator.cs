namespace Core.Interfaces.Crafting
{
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using System.Collections.Generic;

    public interface IItemCreator
    {
        IEquipItem CreateEquipItem(string resultItemId, IEnumerable<IMaterialModifier> resourceModifiers, ICharacter? player = null);
        IItem CreateItem(string resultItemId);
    }
}
