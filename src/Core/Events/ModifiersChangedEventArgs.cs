namespace Core.Events
{
    using System.Collections.Generic;
    using Components;
    using Enums;
    using Modifiers;

    internal class ModifiersChangedEventArgs (EntityParameter parameter, IReadOnlyList<IModifierInstance> modifiers): IModifiersChangedEventArgs
    {
        public IReadOnlyList<IModifierInstance> Modifiers { get; } = modifiers;
        public EntityParameter EntityParameter { get; } = parameter;
    }
}
