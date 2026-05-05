namespace Core.Modifiers
{
    using Interfaces.Entity;

    public interface IModifierInstance : IModifier
    {
        string InstanceId { get; }

        object Source { get; }

        IModifierInstance Copy();

        void Apply(IEntity entity);
        void Remove(IEntity entity);
    }
}
