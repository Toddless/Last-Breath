namespace Core.Interfaces.Components
{
    using Enums;
    using Modifiers;
    using System.Collections.Generic;

    public interface IModifiersChangedEventArgs
    {
        IReadOnlyList<IModifierInstance> Modifiers { get; }
        EntityParameter EntityParameter { get; }
    }
}
