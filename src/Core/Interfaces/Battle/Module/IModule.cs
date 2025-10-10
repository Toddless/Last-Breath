namespace Core.Interfaces.Battle.Module
{
    using Core.Enums;

    public interface IModule : IIdentifiable, IDisplayable
    {
        DecoratorPriority Priority { get; }
    }
}
