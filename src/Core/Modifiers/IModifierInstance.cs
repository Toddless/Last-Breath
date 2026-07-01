namespace Core.Modifiers
{
    using Interfaces.Entity;

    public interface IModifierInstance : IModifier
    {
        string InstanceId { get; }

        string Source { get; }

        IModifierInstance Copy();

        void ApplyTo(IEntity target);
        void RemoveFrom(IEntity target);
    }
}
