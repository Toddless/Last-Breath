namespace LastBreath.Components
{
    using System;
    using Core.Interfaces;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public class ModifiersChangedEventArgs(EntityParameter parameter, IReadOnlyList<IModifierInstance> modifiers) : EventArgs, IModifiersChangedEventArgs
    {
        public EntityParameter Parameter { get; } = parameter;
        public IReadOnlyList<IModifierInstance> Modifiers { get; } = modifiers;
    }
}
