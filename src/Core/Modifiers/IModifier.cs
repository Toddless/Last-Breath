namespace Core.Modifiers
{
    using Enums;
    using Interfaces;

    public interface IModifier : IWeightable
    {
        ModifierValueType ModifierValueType { get; }
        EntityParameter EntityParameter { get; }
        float BaseValue { get; }
        float Value { get; set; }
    }
}
