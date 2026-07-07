namespace Crafting.Internal.Modifiers
{
    using System.Collections.Generic;
    using Core.Components;
    using Core.Enums;
    using Core.Modifiers;

    internal class ModifiersChangedEventArgs(IReadOnlyList<IModifierInstance> modifiers, EntityParameter entityParameter) : IModifiersChangedEventArgs
    {
        public IReadOnlyList<IModifierInstance> Modifiers { get; } = modifiers;

        public EntityParameter EntityParameter { get; } = entityParameter;
    }
}
