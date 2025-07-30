namespace Playground.Components
{
    using System;
    using Playground.Script.Abilities.Modifiers;
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public class ModifiersChangedEventArgs(Parameter parameter, IReadOnlyList<IModifier> modifiers) : EventArgs
    {
        public Parameter Parameter { get; } = parameter;
        public IReadOnlyList<IModifier> Modifiers { get; } = modifiers;
    }
}
