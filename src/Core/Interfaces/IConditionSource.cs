namespace Core.Interfaces
{
    using System;

    public interface IConditionSource<out T>
    {
        T Value { get; }
        event Action Changed;
    }
}
