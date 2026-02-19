namespace Core.Interfaces.Crafting
{
    using Items;
    using Entity;
    using System.Collections.Generic;
    using Modifiers;

    public interface IItemCreator
    {
        IEquipItem CreateEquipItem(string resultItemId, IEnumerable<IModifier> resourceModifiers, IEntity? player = null);
        IItem CreateItem(string resultItemId);
    }
}
