namespace Core.Interfaces.Components.Module
{
    using Enums;

    public interface IModule : IIdentifiable, IDisplayable
    {
        DecoratorPriority Priority { get; }
    }
}
