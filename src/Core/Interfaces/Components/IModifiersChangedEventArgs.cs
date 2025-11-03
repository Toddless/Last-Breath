namespace Core.Interfaces.Components
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces;

    public interface IModifiersChangedEventArgs
    {
        IReadOnlyList<IModifierInstance> Modifiers { get; }
        Parameter Parameter { get; }
    }
}
