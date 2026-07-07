namespace Core.Modifiers
{
    using Entity;

    public interface IModifierInstance : IModifier
    {
        string InstanceId { get; }

        string Source { get; }

        IModifierInstance Copy();

        void ApplyTo(IFightable target);
        void RemoveFrom(IFightable target);
    }
}
