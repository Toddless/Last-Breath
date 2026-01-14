namespace Core.Interfaces.Crafting
{
    using Entity;
    using Items;
    using System.Collections.Generic;

    public interface IItemCreator
    {
        IEquipItem CreateEquipItem(string resultItemId, IEnumerable<IMaterialModifier> resourceModifiers, IEntity? player = null);
        IItem CreateItem(string resultItemId);
    }
}
