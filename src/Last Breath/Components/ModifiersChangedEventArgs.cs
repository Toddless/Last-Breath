namespace LastBreath.Components
{
    using System;
    using System.Collections.Generic;
    using LastBreath.Script.Abilities.Modifiers;
    using LastBreath.Script.Enums;

    public class ModifiersChangedEventArgs(Parameter parameter, IReadOnlyList<IModifier> modifiers) : EventArgs
    {
        public Parameter Parameter { get; } = parameter;
        public IReadOnlyList<IModifier> Modifiers { get; } = modifiers;
    }
}
