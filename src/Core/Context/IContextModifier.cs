namespace Core.Context
{
    using Enums;
    using Interfaces;

    public interface IContextModifier<in T> : IIdentifiable
    {
        ContextModifierPriority Priority { get; }

        void Apply(T context);
    }
}
