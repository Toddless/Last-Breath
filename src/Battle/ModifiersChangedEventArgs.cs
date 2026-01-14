namespace Battle
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Components;

    internal class ModifiersChangedEventArgs (EntityParameter parameter, IReadOnlyList<IModifierInstance> modifiers): IModifiersChangedEventArgs
    {
        public IReadOnlyList<IModifierInstance> Modifiers { get; } = modifiers;
        public EntityParameter EntityParameter { get; } = parameter;
    }
}
