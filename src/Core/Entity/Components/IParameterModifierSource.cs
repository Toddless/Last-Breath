namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using Enums;
    using Modifiers;

    public interface IParameterModifierSource
    {
        event Action<IReadOnlyCollection<EntityParameter>>? SourceChanged;

        IReadOnlyCollection<EntityParameter> AffectedParameters { get; }

        IEnumerable<IModifierInstance> GetModifiers(EntityParameter parameter);
    }
}
