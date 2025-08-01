namespace LastBreath.Components
{
    using System;
    using System.Collections.Generic;
    using Contracts.Enums;
    using LastBreath.Script.Abilities.Modifiers;

    public class ModifiersChangedEventArgs(Parameter parameter, IReadOnlyList<IModifier> modifiers) : EventArgs
    {
        public Parameter Parameter { get; } = parameter;
        public IReadOnlyList<IModifier> Modifiers { get; } = modifiers;
    }
}
