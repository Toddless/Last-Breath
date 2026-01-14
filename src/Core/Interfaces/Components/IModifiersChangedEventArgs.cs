namespace Core.Interfaces.Components
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;

    public interface IModifiersChangedEventArgs
    {
        IReadOnlyList<IModifierInstance> Modifiers { get; }
        EntityParameter EntityParameter { get; }
    }
}
