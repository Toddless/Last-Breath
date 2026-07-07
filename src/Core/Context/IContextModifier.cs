namespace Core.Context
{
    using Enums;
    using Interfaces;

    public interface IContextModifier<in T> : IIdentifiable
    {
        Priority Priority { get; }

        void Apply(T context);
    }
}
