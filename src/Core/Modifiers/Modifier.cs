namespace Core.Modifiers
{
    using Enums;

    public class Modifier(ModifierType type, EntityParameter entityParameter, float baseValue, float weight = 0) : IModifier
    {
        public ModifierType ModifierType { get; } = type;
        public EntityParameter EntityParameter { get; } = entityParameter;
        public float BaseValue { get; } = baseValue;
        public float Value { get; set; } = baseValue;
        public float Weight { get; set; } = weight;
    }
}
