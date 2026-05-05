namespace Core.Modifiers
{
    public interface IConditionalModifier : IModifierInstance
    {
        bool IsActive { get; }
    }
}
