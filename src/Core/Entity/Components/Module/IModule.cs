namespace Core.Entity.Components.Module
{
    using Enums;
    using Interfaces;

    public interface IModule : IIdentifiable, IDisplayable
    {
        Priority Priority { get; }
    }
}
