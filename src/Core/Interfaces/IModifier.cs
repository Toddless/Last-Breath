namespace Core.Interfaces
{
    using Core.Enums;

    public interface IModifier
    {
        ModifierType ModifierType { get; }
        Parameter Parameter { get; }
        float BaseValue { get; }
        float Value { get; set; }
    }
}
