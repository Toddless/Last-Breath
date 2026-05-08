namespace Core.Interfaces.Components.Module
{
    using Enums;

    public interface IModule : IIdentifiable, IDisplayable
    {
        Priority Priority { get; }
    }
}
