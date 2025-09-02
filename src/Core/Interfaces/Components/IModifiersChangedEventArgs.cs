namespace Core.Interfaces.Components
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Modifiers;

    public interface IModifiersChangedEventArgs
    {
        IReadOnlyList<IModifier> Modifiers { get; }
        Parameter Parameter { get; }
    }
}
