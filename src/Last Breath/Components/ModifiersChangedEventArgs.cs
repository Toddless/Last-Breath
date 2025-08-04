namespace LastBreath.Components
{
    using System;
    using System.Collections.Generic;
    using Contracts.Enums;
    using Contracts.Interfaces;

    public class ModifiersChangedEventArgs(Parameter parameter, IReadOnlyList<IModifier> modifiers) : EventArgs
    {
        public Parameter Parameter { get; } = parameter;
        public IReadOnlyList<IModifier> Modifiers { get; } = modifiers;
    }
}
