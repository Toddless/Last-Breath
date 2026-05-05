namespace Core.Modifiers
{
    using System;
    using Interfaces;

    public interface IConditionTarget : IIdentifiable
    {
        event Action<IConditionTarget> StatusChanged;
    }
}
