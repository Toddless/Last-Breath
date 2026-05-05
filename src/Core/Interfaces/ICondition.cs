namespace Core.Interfaces
{
    using System;

    public interface ICondition
    {
        bool IsMet { get; }
        event Action<bool> StateChanged;
    }
}
