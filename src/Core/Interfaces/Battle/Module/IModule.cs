namespace Core.Interfaces.Battle.Module
{
    using Enums;

    public interface IModule : IIdentifiable, IDisplayable
    {
        DecoratorPriority Priority { get; }
    }
}
