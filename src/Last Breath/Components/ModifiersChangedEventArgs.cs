namespace LastBreath.Components
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    public class ModifiersChangedEventArgs(Parameter parameter, IReadOnlyList<IItemModifier> modifiers) : EventArgs, IModifiersChangedEventArgs
    {
        public Parameter Parameter { get; } = parameter;
        public IReadOnlyList<IItemModifier> Modifiers { get; } = modifiers;
    }
}
