namespace Core.Interfaces.Battle.Module
{
    using Enums;

    public interface IParameterModule
    {
        Parameter Parameter { get; }
        DecoratorPriority Priority { get; }

        float GetValue();
    }
}
