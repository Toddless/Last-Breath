namespace Core.Modifiers
{
    using Enums;
    using Interfaces;

    public interface IModifier : IWeightable
    {
        ModifierType ModifierType { get; }
        EntityParameter EntityParameter { get; }
        float BaseValue { get; }
        float Value { get; set; }
    }
}
