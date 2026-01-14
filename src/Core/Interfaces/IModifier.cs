namespace Core.Interfaces
{
    using Enums;

    public interface IModifier
    {
        ModifierType ModifierType { get; }
        EntityParameter EntityParameter { get; }
        float BaseValue { get; }
        float Value { get; set; }
    }
}
