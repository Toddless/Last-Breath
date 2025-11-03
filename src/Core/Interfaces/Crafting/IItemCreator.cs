namespace Core.Interfaces.Crafting
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Items;
    using System.Collections.Generic;

    public interface IItemCreator
    {
        IEquipItem CreateEquipItem(string resultItemId, IEnumerable<IMaterialModifier> resourceModifiers, IEntity? player = null);
        IItem CreateItem(string resultItemId);
    }
}
