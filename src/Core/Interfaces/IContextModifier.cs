namespace Core.Interfaces
{
    using Enums;

    public interface IContextModifier<in T> : IIdentifiable
    {
        Priority Priority { get; }

        void Apply(T context);
    }
}
