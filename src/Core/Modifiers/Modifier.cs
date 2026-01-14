namespace Core.Modifiers
{
    using Enums;
    using Interfaces;

    public class Modifier(ModifierType type, EntityParameter entityParameter, float baseValue) : IModifier
    {
        public ModifierType ModifierType { get; } = type;

        public EntityParameter EntityParameter { get; } = entityParameter;

        public float BaseValue { get; } = baseValue;

        public float Value { get; set; } = baseValue;
    }
}
