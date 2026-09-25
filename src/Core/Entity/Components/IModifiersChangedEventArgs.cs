namespace Core.Entity.Components
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;

    public interface IModifiersChangedEventArgs
    {
        IReadOnlyList<IModifierInstance> Modifiers { get; }
        EntityParameter EntityParameter { get; }
    }
}
