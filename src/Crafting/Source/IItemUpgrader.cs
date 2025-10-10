namespace Crafting.Source
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Results;

    public interface IItemUpgrader
    {
        IModifierInstance TryRecraftModifier(IEquipItem item, int modifierToReroll, IEnumerable<IMaterialModifier> modifiers, ICharacter? player = null);
        IResult<ItemUpgradeResult> TryUpgradeItem(IEquipItem item);
    }
}